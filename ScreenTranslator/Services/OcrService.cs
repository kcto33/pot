using System.Collections.Frozen;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;

namespace ScreenTranslator.Services;

public sealed class OcrService
{
  private const int RetryMinWidthPx = 220;
  private const int RetryMinHeightPx = 72;
  private const int RetryMaxScale = 4;
  private const int RetryPaddingPx = 12;

  private readonly Dictionary<string, OcrEngine> _engines = new(StringComparer.OrdinalIgnoreCase);
  private readonly object _lock = new();
  private OcrEngine? _fallback;

  private static readonly string[] AutoFallbackLanguages =
  [
    "zh-Hans",
    "zh-Hant",
    "ja-JP",
    "ko-KR",
    "en",
  ];

  private static readonly FrozenDictionary<string, string> LanguageMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
  {
    ["zh"] = "zh-Hans",
    ["zh-CN"] = "zh-Hans",
    ["zh-Hans"] = "zh-Hans",
    ["zh-CHS"] = "zh-Hans",
    ["zh-TW"] = "zh-Hant",
    ["zh-Hant"] = "zh-Hant",
    ["zh-CHT"] = "zh-Hant",
    ["ja"] = "ja-JP",
    ["ko"] = "ko-KR",
    ["fr"] = "fr-FR",
    ["de"] = "de-DE",
    ["es"] = "es-ES",
    ["it"] = "it-IT",
    ["ru"] = "ru-RU",
    ["pt"] = "pt-BR",
    ["ar"] = "ar-SA",
    ["hi"] = "hi-IN",
    ["id"] = "id-ID",
    ["th"] = "th-TH",
    ["vi"] = "vi-VN",
  }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

  public async Task<string> RecognizeAsync(Bitmap bitmap, string? languageTag, CancellationToken ct)
  {
    ct.ThrowIfCancellationRequested();

    using var converted = EnsureBgra32(bitmap);
    using var softwareBitmap = ToSoftwareBitmap(converted);

    ct.ThrowIfCancellationRequested();
    var tag = NormalizeLanguage(languageTag);
    var recognizedText = await RecognizeWithLanguageAsync(softwareBitmap, tag, ct);
    if (!string.IsNullOrWhiteSpace(recognizedText) || !ShouldRetryWithEnhancedBitmap(converted))
      return recognizedText;

    using var enhancedBitmap = CreateEnhancedRetryBitmap(converted);
    using var enhancedSoftwareBitmap = ToSoftwareBitmap(enhancedBitmap);
    return await RecognizeWithLanguageAsync(enhancedSoftwareBitmap, tag, ct);
  }

  private async Task<string> RecognizeWithLanguageAsync(SoftwareBitmap softwareBitmap, string? languageTag, CancellationToken ct)
  {
    ct.ThrowIfCancellationRequested();
    if (IsAuto(languageTag))
      return await RecognizeAutoAsync(softwareBitmap, ct);

    var engine = GetEngine(languageTag);
    var result = await engine.RecognizeAsync(softwareBitmap);
    return ExtractText(result);
  }

  private async Task<string> RecognizeAutoAsync(SoftwareBitmap softwareBitmap, CancellationToken ct)
  {
    ct.ThrowIfCancellationRequested();

    using var sharedBitmap = SoftwareBitmap.Copy(softwareBitmap);

    var primary = GetFallbackEngine();
    var primaryResult = await primary.RecognizeAsync(sharedBitmap);
    var primaryText = ExtractText(primaryResult);
    if (!string.IsNullOrWhiteSpace(primaryText))
      return primaryText;

    foreach (var lang in AutoFallbackLanguages)
    {
      ct.ThrowIfCancellationRequested();
      if (!TryGetEngine(lang, out var engine))
        continue;

      var result = await engine.RecognizeAsync(sharedBitmap);
      var text = ExtractText(result);
      if (!string.IsNullOrWhiteSpace(text))
        return text;
    }

    return string.Empty;
  }

  private OcrEngine GetEngine(string? languageTag)
  {
    var tag = NormalizeLanguage(languageTag);
    if (IsAuto(tag))
      return GetFallbackEngine();

    lock (_lock)
    {
      if (_engines.TryGetValue(tag, out var cached))
        return cached;

      var engine = OcrEngine.TryCreateFromLanguage(new Language(tag))
        ?? throw new InvalidOperationException($"Failed to create OCR engine for language '{tag}'.");

      _engines[tag] = engine;
      return engine;
    }
  }

  private static string NormalizeLanguage(string? tag)
  {
    if (string.IsNullOrWhiteSpace(tag))
      return "auto";

    tag = tag.Trim();
    return LanguageMap.TryGetValue(tag, out var mapped) ? mapped : tag;
  }

  private static bool IsAuto(string? tag) =>
    string.IsNullOrWhiteSpace(tag) || string.Equals(tag, "auto", StringComparison.OrdinalIgnoreCase);

  private OcrEngine GetFallbackEngine()
  {
    return _fallback ??= OcrEngine.TryCreateFromUserProfileLanguages()
      ?? OcrEngine.TryCreateFromLanguage(new Language("en"))
      ?? throw new InvalidOperationException("Failed to create OCR engine.");
  }

  private bool TryGetEngine(string? languageTag, out OcrEngine engine)
  {
    engine = default!;
    var tag = NormalizeLanguage(languageTag);
    if (IsAuto(tag))
    {
      engine = GetFallbackEngine();
      return true;
    }

    lock (_lock)
    {
      if (_engines.TryGetValue(tag, out var cached))
      {
        engine = cached;
        return true;
      }

      var created = OcrEngine.TryCreateFromLanguage(new Language(tag));
      if (created is null)
        return false;

      _engines[tag] = created;
      engine = created;
      return true;
    }
  }

  private static Bitmap EnsureBgra32(Bitmap src)
  {
    if (src.PixelFormat == PixelFormat.Format32bppPArgb || src.PixelFormat == PixelFormat.Format32bppArgb)
      return (Bitmap)src.Clone();

    var bmp = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppPArgb);
    using var g = Graphics.FromImage(bmp);
    g.DrawImage(src, 0, 0, src.Width, src.Height);
    return bmp;
  }

  internal static bool ShouldRetryWithEnhancedBitmap(Bitmap bitmap)
  {
    return bitmap.Width < RetryMinWidthPx || bitmap.Height < RetryMinHeightPx;
  }

  internal static Bitmap CreateEnhancedRetryBitmap(Bitmap src)
  {
    var scale = GetRetryScale(src.Width, src.Height);
    var padding = Math.Max(RetryPaddingPx, Math.Min(src.Width, src.Height) / 3);
    var width = (src.Width * scale) + (padding * 2);
    var height = (src.Height * scale) + (padding * 2);
    var background = EstimatePaddingColor(src);

    var bmp = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
    using var g = Graphics.FromImage(bmp);
    g.Clear(background);
    g.CompositingQuality = CompositingQuality.HighQuality;
    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
    g.SmoothingMode = SmoothingMode.HighQuality;
    g.DrawImage(src, new Rectangle(padding, padding, src.Width * scale, src.Height * scale));

    return bmp;
  }

  private static int GetRetryScale(int width, int height)
  {
    var scaleForWidth = (int)Math.Ceiling((double)RetryMinWidthPx / Math.Max(1, width));
    var scaleForHeight = (int)Math.Ceiling((double)RetryMinHeightPx / Math.Max(1, height));
    return Math.Clamp(Math.Max(2, Math.Max(scaleForWidth, scaleForHeight)), 2, RetryMaxScale);
  }

  private static Color EstimatePaddingColor(Bitmap src)
  {
    var samples = new[]
    {
      src.GetPixel(0, 0),
      src.GetPixel(src.Width - 1, 0),
      src.GetPixel(0, src.Height - 1),
      src.GetPixel(src.Width - 1, src.Height - 1),
    };

    var a = 0;
    var r = 0;
    var g = 0;
    var b = 0;

    foreach (var color in samples)
    {
      a += color.A;
      r += color.R;
      g += color.G;
      b += color.B;
    }

    return Color.FromArgb(a / samples.Length, r / samples.Length, g / samples.Length, b / samples.Length);
  }

  private static string ExtractText(OcrResult result)
  {
    if (result.Lines is not { Count: > 0 })
      return (result.Text ?? string.Empty).Trim();

    var builder = new StringBuilder();
    for (var i = 0; i < result.Lines.Count; i++)
    {
      var lineText = result.Lines[i]?.Text?.Trim();
      if (string.IsNullOrWhiteSpace(lineText))
        continue;

      if (builder.Length > 0)
        builder.AppendLine();

      builder.Append(lineText);
    }

    return builder.Length > 0
      ? builder.ToString()
      : (result.Text ?? string.Empty).Trim();
  }

  private static SoftwareBitmap ToSoftwareBitmap(Bitmap bmp)
  {
    var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
    var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
    try
    {
      int bytes = Math.Abs(data.Stride) * data.Height;
      var buffer = new byte[bytes];
      Marshal.Copy(data.Scan0, buffer, 0, bytes);

      var sb = new SoftwareBitmap(BitmapPixelFormat.Bgra8, bmp.Width, bmp.Height, BitmapAlphaMode.Premultiplied);
      sb.CopyFromBuffer(buffer.AsBuffer());
      return sb;
    }
    finally
    {
      bmp.UnlockBits(data);
    }
  }
}

using System.Drawing;
using System.Drawing.Text;
using ScreenTranslator.Services;
using Xunit;

namespace ScreenTranslator.Tests;

public sealed class OcrServiceTests
{
  [Fact]
  public async Task RecognizeAsync_ReturnsText_ForTightSmallWordImage()
  {
    using var bitmap = CreateWordBitmap(width: 90, height: 30, fontSizePx: 12, x: 8, y: 5, text: "youdao");
    var service = new OcrService();

    var result = await service.RecognizeAsync(bitmap, "auto", CancellationToken.None);

    Assert.False(string.IsNullOrWhiteSpace(result));
  }

  [Fact]
  public async Task RecognizeAsync_PreservesLineBreaks_ForMultilineImage()
  {
    using var bitmap = CreateMultilineBitmap();
    var service = new OcrService();

    var result = await service.RecognizeAsync(bitmap, "en", CancellationToken.None);

    Assert.Contains(Environment.NewLine, result);
  }

  private static Bitmap CreateWordBitmap(int width, int height, float fontSizePx, int x, int y, string text)
  {
    var bitmap = new Bitmap(width, height);
    using var graphics = Graphics.FromImage(bitmap);
    using var font = new Font("Segoe UI", fontSizePx, FontStyle.Regular, GraphicsUnit.Pixel);

    graphics.Clear(Color.White);
    graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
    graphics.DrawString(text, font, Brushes.Black, x, y);

    return bitmap;
  }

  private static Bitmap CreateMultilineBitmap()
  {
    var bitmap = new Bitmap(800, 220);
    using var graphics = Graphics.FromImage(bitmap);
    using var font = new Font("Consolas", 30, FontStyle.Regular, GraphicsUnit.Pixel);

    graphics.Clear(Color.White);
    graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
    graphics.DrawString("first line", font, Brushes.Black, 20, 20);
    graphics.DrawString("second line", font, Brushes.Black, 20, 85);
    graphics.DrawString("third line", font, Brushes.Black, 20, 150);

    return bitmap;
  }
}

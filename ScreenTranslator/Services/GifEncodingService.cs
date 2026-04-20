using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ScreenTranslator.Services;

public sealed class GifEncodingService
{
  public byte[] Encode(IReadOnlyList<BitmapSource> frames, int frameIntervalMs)
  {
    if (frames.Count == 0)
    {
      throw new InvalidOperationException("frames must contain at least one image.");
    }

    var encoder = new GifBitmapEncoder();
    var delays = BuildFrameDelays(frameIntervalMs, frames.Count);

    for (var index = 0; index < frames.Count; index++)
    {
      var normalizedFrame = NormalizeFrame(frames[index]);
      var metadata = new BitmapMetadata("gif");
      metadata.SetQuery("/grctlext/Delay", delays[index]);
      metadata.SetQuery("/grctlext/Disposal", (byte)2);

      var frame = BitmapFrame.Create(normalizedFrame, null, metadata, null);
      encoder.Frames.Add(frame);
    }

    using var stream = new MemoryStream();
    encoder.Save(stream);
    var bytes = stream.ToArray();
    PatchGraphicControlExtensions(bytes, delays);
    return bytes;
  }

  internal static IReadOnlyList<ushort> BuildFrameDelays(int frameIntervalMs, int frameCount)
  {
    if (frameCount <= 0)
    {
      return Array.Empty<ushort>();
    }

    var delays = new ushort[frameCount];
    var baseDelay = frameIntervalMs / 10;
    var remainder = frameIntervalMs % 10;
    var accumulator = 0;

    for (var index = 0; index < frameCount; index++)
    {
      var delay = baseDelay;
      accumulator += remainder;
      if (accumulator >= 10)
      {
        delay++;
        accumulator -= 10;
      }

      delays[index] = (ushort)Math.Max(1, delay);
    }

    return delays;
  }

  private static BitmapSource NormalizeFrame(BitmapSource frame)
  {
    if (frame.Format == PixelFormats.Bgra32)
    {
      return frame;
    }

    var converted = new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
    if (converted.CanFreeze)
    {
      converted.Freeze();
    }

    return converted;
  }

  private static void PatchGraphicControlExtensions(byte[] bytes, IReadOnlyList<ushort> delays)
  {
    var delayIndex = 0;

    for (var index = 0; index + 7 < bytes.Length && delayIndex < delays.Count; index++)
    {
      if (bytes[index] != 0x21 || bytes[index + 1] != 0xF9 || bytes[index + 2] != 0x04)
      {
        continue;
      }

      bytes[index + 3] = (byte)((bytes[index + 3] & ~0x1C) | (2 << 2));
      var delay = delays[delayIndex++];
      bytes[index + 4] = (byte)(delay & 0xFF);
      bytes[index + 5] = (byte)(delay >> 8);
    }
  }
}

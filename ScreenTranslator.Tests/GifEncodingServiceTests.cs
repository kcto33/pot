using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScreenTranslator.Services;
using Xunit;

namespace ScreenTranslator.Tests;

public sealed class GifEncodingServiceTests
{
  [Fact]
  public void Encode_Throws_When_Frame_Collection_Is_Empty()
  {
    var service = new GifEncodingService();

    Assert.Throws<InvalidOperationException>(() => service.Encode([], GifRecordingDefaults.FrameIntervalMs));
  }

  [Fact]
  public void BuildFrameDelays_Alternates_12_And_13_Centiseconds_For_125Ms()
  {
    var delays = GifEncodingService.BuildFrameDelays(GifRecordingDefaults.FrameIntervalMs, 4);

    Assert.Equal(new ushort[] { 12, 13, 12, 13 }, delays);
  }

  [Fact]
  public void Encode_Returns_Animated_Gif_With_Expected_Frame_Delays()
  {
    var service = new GifEncodingService();
    var frames = new[]
    {
      CreateSolidFrame(6, 4, Colors.Red),
      CreateSolidFrame(6, 4, Colors.Blue),
    };

    var bytes = service.Encode(frames, GifRecordingDefaults.FrameIntervalMs);

    using var stream = new MemoryStream(bytes);
    var decoder = new GifBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);

    Assert.Equal(2, decoder.Frames.Count);
    Assert.Equal(6, decoder.Frames[0].PixelWidth);
    Assert.Equal(4, decoder.Frames[0].PixelHeight);

    var metadata = Assert.IsType<BitmapMetadata>(decoder.Frames[1].Metadata);
    Assert.Equal((ushort)13, Assert.IsType<ushort>(metadata.GetQuery("/grctlext/Delay")));
  }

  private static BitmapSource CreateSolidFrame(int width, int height, Color color)
  {
    var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
    var pixels = new byte[width * height * 4];

    for (var index = 0; index < pixels.Length; index += 4)
    {
      pixels[index + 0] = color.B;
      pixels[index + 1] = color.G;
      pixels[index + 2] = color.R;
      pixels[index + 3] = color.A;
    }

    bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);
    bitmap.Freeze();
    return bitmap;
  }
}

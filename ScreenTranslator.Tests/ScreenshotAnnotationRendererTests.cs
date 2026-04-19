using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScreenTranslator.Services;
using Xunit;

namespace ScreenTranslator.Tests;

public sealed class ScreenshotAnnotationRendererTests
{
  [Fact]
  public void RenderComposite_Draws_Rectangle_Stroke_Over_Base_Image()
  {
    var baseImage = new WriteableBitmap(40, 40, 96, 96, PixelFormats.Bgra32, null);
    var session = new ScreenshotAnnotationSession(
      new Size(40, 40),
      Geometry.Parse("M0,0 L40,0 40,40 0,40 Z"));

    session.SetActiveTool(ScreenshotAnnotationTool.Rectangle);
    session.CommitRectangle(new Rect(5, 5, 20, 10), Colors.Red, 4);

    var result = ScreenshotAnnotationRenderer.RenderComposite(baseImage, session);

    Assert.Equal(40, result.PixelWidth);
    Assert.Equal(40, result.PixelHeight);
    Assert.NotEqual(GetPixel(baseImage, 5, 5), GetPixel(result, 5, 5));
    Assert.Equal(GetPixel(baseImage, 0, 0), GetPixel(result, 0, 0));
  }

  [Fact]
  public void RenderComposite_Applies_Mosaic_To_Selected_Region_Only()
  {
    var baseImage = CreateGradientImage(32, 32);
    var session = new ScreenshotAnnotationSession(
      new Size(32, 32),
      Geometry.Parse("M0,0 L32,0 32,32 0,32 Z"));

    session.SetActiveTool(ScreenshotAnnotationTool.Mosaic);
    session.CommitStroke(
      [
        new Point(4, 4),
        new Point(8, 8),
        new Point(12, 12)
      ],
      Colors.Transparent,
      strokeThickness: 10);

    var result = ScreenshotAnnotationRenderer.RenderComposite(baseImage, session);

    Assert.Equal(32, result.PixelWidth);
    Assert.Equal(32, result.PixelHeight);
    Assert.NotEqual(GetPixel(baseImage, 8, 8), GetPixel(result, 8, 8));
    Assert.Equal(GetPixel(baseImage, 0, 0), GetPixel(result, 0, 0));
  }

  private static WriteableBitmap CreateGradientImage(int width, int height)
  {
    var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
    var pixels = new byte[width * height * 4];

    for (var y = 0; y < height; y++)
    {
      for (var x = 0; x < width; x++)
      {
        var index = (y * width + x) * 4;
        pixels[index + 0] = (byte)(x * 7);
        pixels[index + 1] = (byte)(y * 7);
        pixels[index + 2] = (byte)((x + y) * 4);
        pixels[index + 3] = 255;
      }
    }

    bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);
    return bitmap;
  }

  private static uint GetPixel(BitmapSource source, int x, int y)
  {
    var pixels = new byte[4];
    source.CopyPixels(new Int32Rect(x, y, 1, 1), pixels, 4, 0);
    return BitConverter.ToUInt32(pixels, 0);
  }
}

using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScreenTranslator.Windows;
using Xunit;

namespace ScreenTranslator.Tests;

public sealed class ScreenshotOverlayWindowTests
{
  [Fact]
  public void ShouldAssignCapturedBackground_ReturnsFalse_WhenWindowIsClosed()
  {
    var bitmap = new WriteableBitmap(1, 1, 96, 96, PixelFormats.Bgra32, null);

    var shouldAssign = ScreenshotOverlayWindow.ShouldAssignCapturedBackground(isClosed: true, bitmap);

    Assert.False(shouldAssign);
  }

  [Fact]
  public void ShouldAssignCapturedBackground_ReturnsFalse_WhenBitmapIsMissing()
  {
    var shouldAssign = ScreenshotOverlayWindow.ShouldAssignCapturedBackground(isClosed: false, null);

    Assert.False(shouldAssign);
  }

  [Fact]
  public void ShouldAssignCapturedBackground_ReturnsTrue_WhenWindowIsOpenAndBitmapExists()
  {
    var bitmap = new WriteableBitmap(1, 1, 96, 96, PixelFormats.Bgra32, null);

    var shouldAssign = ScreenshotOverlayWindow.ShouldAssignCapturedBackground(isClosed: false, bitmap);

    Assert.True(shouldAssign);
  }
}

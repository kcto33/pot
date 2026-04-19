using ScreenTranslator.Models;
using ScreenTranslator.Services;
using Xunit;

using WinRect = System.Drawing.Rectangle;

namespace ScreenTranslator.Tests;

public sealed class LongScreenshotCoordinatorTests
{
  [Fact]
  public void ConfigureCaptureHooks_AssignsBeforeAndAfterCaptureCallbacks()
  {
    var session = new LongScreenshotSession(new WinRect(0, 0, 100, 100), new LongScreenshotSettings());
    var beforeCalled = false;
    var afterCalled = false;

    LongScreenshotSessionCoordinator.ConfigureCaptureHooks(
      session,
      () => beforeCalled = true,
      () => afterCalled = true);

    session.BeforeCapture?.Invoke();
    session.AfterCapture?.Invoke();

    Assert.True(beforeCalled);
    Assert.True(afterCalled);
  }

  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public void ShouldAutoCopyResult_FollowsScreenshotAutoCopySetting(bool enabled)
  {
    var settings = new AppSettings
    {
      ScreenshotAutoCopy = enabled,
    };

    var shouldAutoCopy = LongScreenshotSessionCoordinator.ShouldAutoCopyResult(settings);

    Assert.Equal(enabled, shouldAutoCopy);
  }
}

using ScreenTranslator.Services;
using Xunit;

namespace ScreenTranslator.Tests;

public sealed class SelectionFlowControllerTests
{
  [Fact]
  public async Task StartSelectionOrTranslateSelectedTextAsync_UsesSelectedText_WhenCaptureSucceeds()
  {
    var settings = new SettingsService();
    var usedSelectedText = false;
    var startedOverlay = false;
    var controller = new SelectionFlowController(
      settings,
      tryCaptureSelectedTextAsync: _ => Task.FromResult<string?>("selected"),
      startOverlaySelection: () => startedOverlay = true,
      showSelectedTextTranslationAsync: (text, _) =>
      {
        usedSelectedText = text == "selected";
        return Task.CompletedTask;
      });

    await controller.StartSelectionOrTranslateSelectedTextAsync();

    Assert.True(usedSelectedText);
    Assert.False(startedOverlay);
  }

  [Fact]
  public async Task StartSelectionOrTranslateSelectedTextAsync_StartsOverlay_WhenCaptureFails()
  {
    var settings = new SettingsService();
    var startedOverlay = false;
    var controller = new SelectionFlowController(
      settings,
      tryCaptureSelectedTextAsync: _ => Task.FromResult<string?>(null),
      startOverlaySelection: () => startedOverlay = true,
      showSelectedTextTranslationAsync: (_, _) => Task.CompletedTask);

    await controller.StartSelectionOrTranslateSelectedTextAsync();

    Assert.True(startedOverlay);
  }
}

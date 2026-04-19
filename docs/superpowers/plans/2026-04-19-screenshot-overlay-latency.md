# Screenshot Overlay Latency Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the normal screenshot overlay appear immediately after the screenshot hotkey is pressed by moving the full-screen capture off the critical startup path.

**Architecture:** Keep the change local to `ScreenshotOverlayWindow`. The window will initialize and show its dark overlay immediately, then start screen capture asynchronously and assign the frozen background only when that capture completes safely.

**Tech Stack:** .NET 8, WPF, xUnit

---

### Task 1: Add Regression Tests For Delayed Background Assignment

**Files:**
- Modify: `F:/yys/transtools/pot/ScreenTranslator.Tests/LongScreenshotCoordinatorTests.cs`
- Create: `F:/yys/transtools/pot/ScreenTranslator.Tests/ScreenshotOverlayWindowTests.cs`
- Test: `F:/yys/transtools/pot/ScreenTranslator.Tests/ScreenshotOverlayWindowTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using System.Windows.Media.Imaging;
using ScreenTranslator.Windows;
using Xunit;

namespace ScreenTranslator.Tests;

public sealed class ScreenshotOverlayWindowTests
{
  [Fact]
  public void ShouldAssignCapturedBackground_ReturnsFalse_WhenWindowIsClosed()
  {
    var bitmap = new WriteableBitmap(1, 1, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);

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
    var bitmap = new WriteableBitmap(1, 1, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);

    var shouldAssign = ScreenshotOverlayWindow.ShouldAssignCapturedBackground(isClosed: false, bitmap);

    Assert.True(shouldAssign);
  }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test .\ScreenTranslator.Tests\ScreenTranslator.Tests.csproj -c Release --filter FullyQualifiedName~ScreenshotOverlayWindowTests`
Expected: FAIL with missing `ShouldAssignCapturedBackground`

- [ ] **Step 3: Write minimal implementation**

```csharp
internal static bool ShouldAssignCapturedBackground(bool isClosed, BitmapSource? bitmap)
{
  return !isClosed && bitmap is not null;
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test .\ScreenTranslator.Tests\ScreenTranslator.Tests.csproj -c Release --filter FullyQualifiedName~ScreenshotOverlayWindowTests`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add ScreenTranslator.Tests/ScreenshotOverlayWindowTests.cs ScreenTranslator/Windows/ScreenshotOverlayWindow.xaml.cs
git commit -m "test: cover delayed screenshot background assignment"
```

### Task 2: Move Normal Screenshot Background Capture Off The Startup Path

**Files:**
- Modify: `F:/yys/transtools/pot/ScreenTranslator/Windows/ScreenshotOverlayWindow.xaml.cs`
- Test: `F:/yys/transtools/pot/ScreenTranslator.Tests/ScreenshotOverlayWindowTests.cs`

- [ ] **Step 1: Update startup flow so overlay appears before background capture**

```csharp
private void OnLoaded(object sender, RoutedEventArgs e)
{
  var source = PresentationSource.FromVisual(this);
  if (source?.CompositionTarget != null)
  {
    _dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
    _dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
  }

  Left = SystemParameters.VirtualScreenLeft;
  Top = SystemParameters.VirtualScreenTop;
  Width = SystemParameters.VirtualScreenWidth;
  Height = SystemParameters.VirtualScreenHeight;

  UpdateDarkOverlay(null);
  Focus();
  Cursor = WpfCursors.Cross;

  BeginCaptureAllScreensAsync();
}
```

- [ ] **Step 2: Implement async background capture with safe UI assignment**

```csharp
private async void BeginCaptureAllScreensAsync()
{
  var bitmap = await Task.Run(CaptureAllScreensBitmapSource);
  await Dispatcher.BeginInvoke(() =>
  {
    if (!ShouldAssignCapturedBackground(_isClosed, bitmap))
    {
      return;
    }

    _capturedScreen = bitmap;
    BackgroundImage.Source = _capturedScreen;
  });
}
```

- [ ] **Step 3: Keep close-state tracking local to the window**

```csharp
private bool _isClosed;

protected override void OnClosed(EventArgs e)
{
  _isClosed = true;
  base.OnClosed(e);
}
```

- [ ] **Step 4: Run focused tests**

Run: `dotnet test .\ScreenTranslator.Tests\ScreenTranslator.Tests.csproj -c Release --filter FullyQualifiedName~ScreenshotOverlayWindowTests`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add ScreenTranslator/Windows/ScreenshotOverlayWindow.xaml.cs ScreenTranslator.Tests/ScreenshotOverlayWindowTests.cs
git commit -m "feat: show screenshot overlay before background capture completes"
```

### Task 3: Verify Build And Behavior Safety

**Files:**
- Verify: `F:/yys/transtools/pot/ScreenTranslator/Windows/ScreenshotOverlayWindow.xaml.cs`
- Verify: `F:/yys/transtools/pot/ScreenTranslator.Tests/ScreenshotOverlayWindowTests.cs`

- [ ] **Step 1: Run the full test project**

Run: `dotnet test .\ScreenTranslator.Tests\ScreenTranslator.Tests.csproj -c Release`
Expected: PASS

- [ ] **Step 2: Run the application build**

Run: `dotnet build .\ScreenTranslator\ScreenTranslator.csproj -c Release`
Expected: PASS with 0 errors

- [ ] **Step 3: Manual check**

Run: `dotnet run --project .\ScreenTranslator\ScreenTranslator.csproj`
Expected:
- screenshot hotkey shows dark overlay immediately
- frozen background fills in shortly after
- rectangular selection still supports pin/copy/save

- [ ] **Step 4: Commit**

```bash
git add ScreenTranslator/Windows/ScreenshotOverlayWindow.xaml.cs ScreenTranslator.Tests/ScreenshotOverlayWindowTests.cs
git commit -m "chore: verify screenshot overlay latency fix"
```

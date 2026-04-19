# Selected Text Translation Mode Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the existing translate hotkey translate currently selected text first, then fall back to the existing screen-region overlay flow when no selected text is available within 120ms.

**Architecture:** Add a focused selected-text capture service that snapshots and restores clipboard state, sends a copy chord to the foreground window, and returns trimmed text when capture succeeds. Keep `SelectionFlowController` responsible for deciding between selected-text translation and overlay selection, and reuse the existing translation bubble with mouse-based placement for selected text.

**Tech Stack:** .NET 8, WPF, Win32 input/clipboard interop, xUnit

---

### Task 1: Add failing tests for clipboard-driven selected text capture

**Files:**
- Create: `F:/yys/transtools/pot/ScreenTranslator.Tests/SelectedTextCaptureServiceTests.cs`
- Modify: `F:/yys/transtools/pot/ScreenTranslator/Interop/NativeMethods.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
[Fact]
public async Task TryCaptureAsync_ReturnsTrimmedSelectedText_AndRestoresClipboard()
{
  // Arrange clipboard snapshot, foreground window, and injected copy result
  // Act: await service.TryCaptureAsync(...)
  // Assert: selected text returned, original clipboard restored, app clipboard history suppressed
}

[Fact]
public async Task TryCaptureAsync_ReturnsNull_WhenCopyDoesNotProduceTextWithinTimeout()
{
  // Arrange no clipboard change
  // Act: await service.TryCaptureAsync(...)
  // Assert: null result and original clipboard restored
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test .\ScreenTranslator.Tests\ScreenTranslator.Tests.csproj -c Release --filter FullyQualifiedName~SelectedTextCaptureServiceTests`
Expected: FAIL because `SelectedTextCaptureService` does not exist yet

- [ ] **Step 3: Write minimal implementation**

```csharp
public sealed class SelectedTextCaptureService
{
  public Task<string?> TryCaptureAsync(CancellationToken ct) => Task.FromResult<string?>(null);
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test .\ScreenTranslator.Tests\ScreenTranslator.Tests.csproj -c Release --filter FullyQualifiedName~SelectedTextCaptureServiceTests`
Expected: PASS

### Task 2: Add failing tests for translate-hotkey routing

**Files:**
- Modify: `F:/yys/transtools/pot/ScreenTranslator.Tests/TranslationServiceTests.cs`
- Create: `F:/yys/transtools/pot/ScreenTranslator.Tests/SelectionFlowControllerTests.cs`
- Modify: `F:/yys/transtools/pot/ScreenTranslator/Services/SelectionFlowController.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
[Fact]
public async Task StartSelectionAsync_UsesSelectedText_WhenCaptureSucceeds()
{
  // Assert no overlay shown and translation bubble gets selected-text translation
}

[Fact]
public async Task StartSelectionAsync_FallsBackToOverlay_WhenCaptureReturnsNull()
{
  // Assert overlay path still starts
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test .\ScreenTranslator.Tests\ScreenTranslator.Tests.csproj -c Release --filter FullyQualifiedName~SelectionFlowControllerTests`
Expected: FAIL because selected-text-aware flow does not exist yet

- [ ] **Step 3: Write minimal implementation**

```csharp
public async Task StartSelectionOrTranslateSelectedTextAsync()
{
  var selectedText = await _selectedTextCapture.TryCaptureAsync(CancellationToken.None);
  if (!string.IsNullOrWhiteSpace(selectedText))
  {
    await ShowSelectedTextTranslationAsync(selectedText);
    return;
  }

  StartSelectionOverlay();
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test .\ScreenTranslator.Tests\ScreenTranslator.Tests.csproj -c Release --filter FullyQualifiedName~SelectionFlowControllerTests`
Expected: PASS

### Task 3: Wire the new flow into app startup and final verification

**Files:**
- Modify: `F:/yys/transtools/pot/ScreenTranslator/App.xaml.cs`
- Modify: `F:/yys/transtools/pot/ScreenTranslator/Services/ClipboardHistoryService.cs`
- Modify: `F:/yys/transtools/pot/ScreenTranslator/Resources/Strings.en.xaml`
- Modify: `F:/yys/transtools/pot/ScreenTranslator/Resources/Strings.zh-CN.xaml`

- [ ] **Step 1: Integrate app wiring**

```csharp
_flow = new SelectionFlowController(..., clipboardHistoryService: _clipboardHistory);
_tray.StartSelectionRequested += async (_, _) => await _flow.StartSelectionOrTranslateSelectedTextAsync();
_hotkeys.HotkeyPressedById += async (_, id) => { ... };
```

- [ ] **Step 2: Run focused regression checks**

Run: `dotnet test .\ScreenTranslator.Tests\ScreenTranslator.Tests.csproj -c Release`
Expected: PASS

- [ ] **Step 3: Run build verification**

Run: `dotnet build .\ScreenTranslator\ScreenTranslator.csproj -c Release`
Expected: `Build succeeded.`

# Screenshot Overlay Latency Design

Date: 2026-04-19

## Goal

Reduce the perceived latency between pressing the screenshot hotkey and seeing the normal screenshot overlay.

Scope is intentionally limited to the standard rectangular screenshot flow opened by `ScreenshotController -> ScreenshotOverlayWindow`.

Out of scope:

- long screenshot flow
- freeform screenshot flow
- changing the capture backend
- broader screenshot architecture refactors

## Problem

`ScreenshotOverlayWindow` currently performs a full virtual-screen capture synchronously inside `OnLoaded` before the window finishes becoming usable. That means the hotkey path blocks on `CaptureAllScreens()` before the user sees the overlay.

This creates a visible delay on hotkey press, especially on larger desktops or higher DPI setups.

## Chosen Approach

Show the overlay immediately, then populate the frozen desktop background asynchronously.

Behavior:

- The window opens and applies its bounds, dark overlay, focus, and cursor immediately.
- The full-screen capture starts only after the window is already shown.
- Once capture finishes, the captured background is assigned back on the UI thread.

This improves responsiveness by moving the expensive screen grab off the critical path for initial visibility.

## Alternatives Considered

### 1. Prewarm and reuse the overlay window

Pros:

- fastest first-visible response after warmup

Cons:

- higher state-management complexity
- more risk of stale selection state, stale DPI state, or focus issues

### 2. Replace the capture backend

Pros:

- potentially larger raw capture performance gain

Cons:

- much larger change surface
- not needed for the current narrow goal

## Design Details

### Window startup path

`ScreenshotOverlayWindow.OnLoaded` will be reordered so that it:

1. resolves DPI
2. sets window bounds to the virtual desktop
3. initializes the dark overlay
4. sets focus and cursor
5. schedules background capture asynchronously

The synchronous `CaptureAllScreens()` call will be removed from the initial loaded path.

### Background capture path

The capture work will run asynchronously and assign `BackgroundImage.Source` only after the bitmap has been prepared.

The expected implementation shape is:

- capture on a worker thread
- convert to a frozen `BitmapSource`
- marshal back to the UI thread for assignment

### User-visible state before capture completes

The overlay is allowed to appear first as a darkened full-screen layer without the frozen desktop image.

This is acceptable for the requested optimization.

Selection gestures may begin immediately. Actions that require `_capturedScreen` must remain safe if capture is not ready yet.

### Safety rules

Until the background image exists:

- crop operations must continue to return `null` safely
- `Pin`, `Copy`, `Save`, and dependent actions must not throw
- no long-screenshot behavior is changed in this task

The simplest acceptable behavior is to keep current null-guard semantics.

## Testing

Automated coverage for this change should focus on the new logic boundaries, not on full WPF UI automation.

Minimum checks:

- a unit test for the helper deciding whether delayed background assignment is safe when capture is not yet ready
- build verification for the main project

Manual verification:

1. press the screenshot hotkey
2. confirm the dark overlay appears immediately
3. confirm the background image fills in shortly after
4. confirm rectangular selection, copy, pin, and save still work

## Risks

- a user may begin selecting before the background image is ready, which means a very brief mismatch between visible overlay and frozen image timing
- if the async capture path updates UI incorrectly, cross-thread access bugs are possible

These risks are contained by keeping the change local to `ScreenshotOverlayWindow` and preserving existing crop null-guards.

## Recommendation

Implement the async background-capture path now as the smallest high-signal fix for perceived screenshot latency.

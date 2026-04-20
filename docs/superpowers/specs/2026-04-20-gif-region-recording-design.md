# Region GIF Recording Design

## Goal

Add a region-based GIF recording flow that starts from the existing screenshot selection experience. Users will press the current screenshot hotkey, drag to select a region, click a new `GIF` toolbar action, record that fixed region, and save the result as a `.gif` file.

## Scope

- Reuse the existing screenshot hotkey and rectangular region selection flow.
- Add a `GIF` action to the screenshot edit toolbar.
- Record only the selected rectangular region.
- Hide overlay UI during frame capture so the overlay and control windows are not burned into the GIF.
- Show a lightweight recording control window with elapsed time, max duration, and `Stop` / `Cancel`.
- Enforce V1 limits of `30` seconds maximum duration and `8 FPS`.
- Save by opening a `.gif` save dialog immediately after a successful stop.
- Add tests for toolbar ordering, recording-session rules, and GIF encoding behavior.

## Non-Goals

- No standalone GIF hotkey in V1.
- No settings UI for GIF frame rate, duration, quality, or output path behavior in V1.
- No editing, annotation, or redraw while recording is running.
- No result preview window before saving in V1.
- No external dependency such as `ffmpeg`.
- No attempt to support freeform regions; this feature is rectangular-only.

## Chosen Approach

### Options considered

1. Reuse the current screenshot flow and add a `GIF` action after region selection.
2. Add a separate global GIF hotkey and a dedicated recording overlay.
3. Shell out to an external tool such as `ffmpeg` to produce GIF files.

### Decision

Use option `1` and keep encoding in-process. This preserves the current user mental model, keeps the feature inside the existing screenshot workflow, and avoids adding a packaging and distribution dependency for an external encoder. The implementation will use the existing capture pipeline plus WPF/WIC GIF encoding support to write animated GIF output.

## User Experience

### Entry

1. User presses the existing screenshot hotkey.
2. User drags to select a rectangular region in the current screenshot overlay.
3. The overlay enters edit mode and shows the toolbar.
4. The toolbar includes a new `GIF` button placed after `Long Screenshot`.

### Recording

1. User clicks `GIF`.
2. Any screenshot annotations for the current edit session are discarded for GIF recording.
3. The screenshot overlay closes so it cannot appear in captured frames.
4. A dedicated GIF recording session starts with:
   - a locked selection frame around the chosen region
   - a compact non-activating control window near the region
5. The control window shows:
   - elapsed time and max duration, for example `00:04 / 00:30`
   - fixed text showing `8 FPS`
   - `Stop`
   - `Cancel`
6. Recording stops when:
   - the user clicks `Stop`, or
   - elapsed time reaches `30` seconds

### Completion

1. On `Stop`, captured frames are encoded into a GIF in memory or a temporary stream.
2. A save dialog opens immediately with `.gif` as the default extension.
3. The default file name format is `GifRecording_{0:yyyyMMdd_HHmmss}.gif`.
4. The default directory matches the current screenshot save path if configured and valid; otherwise it falls back to the user's Pictures folder.
5. On successful save, the session closes cleanly.
6. On `Cancel`, the session closes without encoding or saving anything.

## Architecture

### Existing components to reuse

- `ScreenshotController` remains the main entry point for screenshot-related flows.
- `SelectionFrameWindow` is reused to display a locked capture region during recording.
- Existing screen capture helpers remain the source of frame pixels.
- The current localization and save-path logic patterns should be reused where practical.

### New components

#### `GifRecordingSessionCoordinator`

Owns the full GIF recording lifecycle.

Responsibilities:

- start and stop the recording session
- create and position the selection frame and control window
- manage capture hooks that temporarily hide visible UI during each frame capture
- coordinate cancellation, auto-stop, encoding, save dialog, and disposal

This should mirror the role that `LongScreenshotSessionCoordinator` plays for long screenshots.

#### `GifRecordingControlWindow`

Displays recording status and accepts user commands.

Responsibilities:

- show elapsed time and max duration
- show the fixed `8 FPS` label
- raise `StopRequested` and `CancelRequested`
- stay outside the capture region and outside task switching noise
- avoid being captured by `CopyFromScreen`

The visual language should stay close to `LongScreenshotControlWindow` so GIF recording feels like part of the same product family.

#### `GifRecordingService`

Runs the timed frame capture loop on a background workflow.

Responsibilities:

- capture the selected region every `125 ms`
- stop at `30` seconds or on explicit stop/cancel
- keep frames ordered
- expose progress updates to the coordinator
- tolerate isolated capture failures and report terminal failure when repeated capture errors make output unreliable

#### `GifEncodingService`

Encodes captured frames into an animated GIF.

Responsibilities:

- reject empty frame collections
- normalize all frames to a consistent size and pixel format
- write frame delay metadata so playback matches `8 FPS`
- output a valid `.gif` byte stream or file

## Data Flow

1. `ScreenshotOverlayWindow` raises the new GIF action from the toolbar.
2. `ScreenshotController.StartGifRecording(...)` closes the overlay flow and creates `GifRecordingSessionCoordinator`.
3. The coordinator shows `SelectionFrameWindow` in locked mode and shows `GifRecordingControlWindow`.
4. The coordinator starts `GifRecordingService`.
5. Before each frame capture, the coordinator hides windows that could leak into the frame.
6. `GifRecordingService` captures the selected region and appends the frame.
7. After each capture, hidden UI is restored.
8. The coordinator receives progress updates and updates the control window timer.
9. When recording stops, the coordinator passes collected frames to `GifEncodingService`.
10. The coordinator opens the save dialog and writes the final GIF.
11. All temporary UI, hooks, and in-memory frame state are disposed.

## Toolbar and UI Changes

### Screenshot toolbar

Update the screenshot toolbar order from:

- `Save`
- `Copy`
- `Long Screenshot`
- `Redraw`
- `Pin`
- `Brush`
- `Rectangle`
- `Mosaic`
- `Undo`
- `Cancel`

To:

- `Save`
- `Copy`
- `Long Screenshot`
- `GIF`
- `Redraw`
- `Pin`
- `Brush`
- `Rectangle`
- `Mosaic`
- `Undo`
- `Cancel`

### Recording window behavior

- The recording control window must be non-activating like the long-screenshot controls.
- The control window must be positioned near the region but outside it when possible.
- The control window must ignore accidental startup clicks for a short debounce window, matching the defensive pattern already used by the long-screenshot control window.
- The selection frame remains locked during recording. Region moves or resizes are not allowed after recording starts.

## Capture and Encoding Rules

### Frame rate and duration

- Fixed frame interval: `125 ms`
- Fixed frame rate target: `8 FPS`
- Fixed max duration: `30 s`
- Max expected raw captured frame count before any dropped-frame optimization: `240`

### Frame acquisition

- Capture uses the selected physical-pixel rectangle, not the edit-surface DIP rectangle.
- The capture loop must operate off the UI thread.
- UI hide/show hooks must run on the UI thread around every capture.
- The selection frame should be hidden for capture if necessary and restored immediately after each frame.
- The control window should be excluded from capture or hidden during capture, whichever is more reliable on the target API path.

### File size controls

V1 size control relies on bounded inputs instead of exposing quality knobs.

- region-only capture
- fixed `8 FPS`
- fixed `30 s` cap
- no full-screen fallback

Optional implementation optimization within V1:

- if two adjacent captured frames are byte-identical after normalization, GIF encoding may drop duplicates and accumulate delay into the surviving frame

That optimization is allowed but not required for the first implementation.

## Settings and Localization

### Settings

Do not add new user-configurable settings in V1.

Reuse existing settings only for:

- screenshot save directory fallback
- screenshot file naming conventions as a style reference
- localization resource access

GIF recording constants remain code-defined for V1:

- max duration: `30`
- fps: `8`
- default extension: `.gif`
- default base file name: `GifRecording`

### Localization

Add localized strings for:

- toolbar button text and tooltip for `GIF`
- recording window labels and hints
- save dialog filter including GIF support
- failure messages related to recording and GIF saving

## Error Handling

### Start validation

- If the selected region is below the current screenshot minimum size threshold, do not start a GIF session.
- If the app cannot initialize the session UI or capture service, abort early and show a short failure message.

### Capture errors

- A single frame capture failure should not immediately fail the session.
- Repeated capture failures should stop the session and present a concise failure hint.
- `Cancel` always wins over any background completion work that finishes after cancellation.

### Encoding errors

- Empty or unusable frame sets must fail fast without opening a misleading save dialog.
- Encoding failure must close the session UI and show a short error message.

### Save errors

- If the user cancels the save dialog, treat that as a clean user cancel after encoding.
- If file writing fails, show a short error message and close the session.

## Testing Design

### Update existing tests

- Extend `ScreenshotOverlayWindowTests` to assert the toolbar order now includes `GIF` after `LongScreenshot`.

### New tests

#### `GifRecordingCoordinatorTests`

- auto-stop occurs when elapsed duration reaches `30` seconds
- cancel closes without encoding or saving
- stop transitions into encoding and save flow
- capture hooks hide and restore recording UI around frame capture

#### `GifEncodingServiceTests`

- empty frame collection is rejected
- multiple frames produce non-empty GIF output
- encoded result preserves frame dimensions
- configured frame delay maps to the `8 FPS` timing model

#### `GifRecordingServiceTests`

- interval scheduling requests captures at `125 ms` cadence
- max duration stops the loop at the expected frame budget
- isolated capture failures can be skipped
- repeated capture failures produce terminal failure

## Verification

- `dotnet build .\ScreenTranslator\ScreenTranslator.csproj -c Release`
- `dotnet test .\ScreenTranslator.Tests\ScreenTranslator.Tests.csproj -c Release`

Manual verification:

1. Run the app.
2. Trigger screenshot mode with the existing screenshot hotkey.
3. Select a region and confirm the toolbar shows the new `GIF` action in the agreed position.
4. Start GIF recording and confirm the overlay is not visible in the recorded output.
5. Let one run auto-stop at `30` seconds.
6. Let one run stop manually before `30` seconds.
7. Confirm both save as playable `.gif` files.

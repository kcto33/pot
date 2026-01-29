# History Paste (Clipboard) Spec

This doc captures the agreed behavior + implementation notes for the "history paste" feature so another agent can implement/finish it consistently.

## User-Facing Behavior

- **Feature name (UI):** `历史粘贴`.
- **Trigger:** global hotkey (default `Ctrl+Shift+V`) and a tray menu item `历史粘贴`.
- **Hotkey is user-configurable** in Settings (separate from the existing screenshot-translate hotkey).
- **Clipboard history:** keep **up to 3** most-recent text entries.
  - If fewer than 3 exist, show only that many.
  - If **0** exist, do **nothing** (no window, no placeholder UI).
- **Picker UI:** a small non-activating popup near the mouse cursor.
  - Use **keyboard** to select: Up/Down changes selection, Enter confirms, Esc cancels.
  - **Click outside** the popup closes it.
  - Visual style should be consistent with `BubbleWindow` and reuse `BubbleSettings` (colors, font, radius, padding).
- **Preview rendering:** each entry is **single-line**.
  - Replace newlines with spaces, collapse whitespace, truncate to a compact length (agreed: **60 chars**) and append `...`.
- **Confirm action:** on Enter, put selected text into clipboard then paste into the currently focused app.
  - Do **not** intercept the system native `Ctrl+V`.

## Technical Constraints / Notes

- This app is a WPF tray app; it has no main window, so clipboard listening requires a hidden message window.
- Clipboard API is flaky under contention; expect `COMException`/`ExternalException` and retry briefly.
- Non-activating popup cannot rely on focus events; outside-click close should use a low-level mouse hook (same pattern as `BubbleWindow`).
- While popup is visible, suppress Up/Down/Enter/Esc from reaching the foreground app (use a temporary low-level keyboard hook).

## Suggested Architecture (Repo-Conformant)

- `ScreenTranslator/Services/ClipboardHistoryService.cs`
  - Listen for `WM_CLIPBOARDUPDATE` via `AddClipboardFormatListener` on an `HwndSource` created with `HWND_MESSAGE` parent.
  - Maintain a small in-memory list/queue (max 3, newest first).
  - Only capture text.
  - De-dupe (ignore if identical to the newest).
  - Provide `GetRecent()` and `SetClipboardTextAsync()` with an internal "suppress once" mechanism to avoid feedback loops.

- `ScreenTranslator/Windows/PasteHistoryWindow.xaml` + `.xaml.cs`
  - WPF popup UI with `ShowActivated="False"`, `Focusable="False"` and Win32 `WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW`.
  - Use `BubbleSettings` for visual styling.
  - Items list binds to 1..3 entries only.
  - Install low-level mouse hook to close on outside click.

- `ScreenTranslator/Services/PasteHistoryController.cs`
  - Entry point `ShowOrClose()`.
  - If history empty => return (no UI).
  - Show popup near cursor (DPI-aware).
  - While popup visible, install `WH_KEYBOARD_LL` and swallow Up/Down/Enter/Esc.
  - On Enter: call `ClipboardHistoryService.SetClipboardTextAsync(selected)` then send `Ctrl+V` via `SendInput`.
  - Cleanup hooks on close.

- `ScreenTranslator/Services/HotkeyService.cs`
  - Must support **multiple hotkeys** (multiple IDs) so screenshot hotkey and history-paste hotkey can coexist.
  - Route `WM_HOTKEY` by id; expose an id-based event.

- `ScreenTranslator/Services/TrayService.cs`
  - Add tray item `历史粘贴`.
  - Display current hotkey string as text (recommended via `\t` alignment).
  - Clicking it triggers the same action as the hotkey.

- `ScreenTranslator/Windows/SettingsWindow.xaml` + `.xaml.cs`
  - Add a second hotkey input labeled `历史粘贴`.
  - Validate:
    - The hotkey string parses.
    - It can be registered.
    - It is not equal to the screenshot hotkey.
  - Persist in settings (new field).

## Win32 / Interop Requirements

- Clipboard listening:
  - `WM_CLIPBOARDUPDATE`, `AddClipboardFormatListener`, `RemoveClipboardFormatListener`.
- Non-activating popup:
  - `GetWindowLong`/`SetWindowLong` for `WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW`.
- Hooks:
  - `WH_MOUSE_LL` (already used) for outside-click close.
  - `WH_KEYBOARD_LL` for navigation keys while popup is visible.
- Paste injection:
  - `SendInput` to send `Ctrl+V`.

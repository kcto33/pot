using System.Windows;

namespace ScreenTranslator;

public partial class App : System.Windows.Application
{
  private const int PasteHistoryHotkeyId = 0xBEEE;

  private Services.SettingsService? _settings;
  private Services.TrayService? _tray;
  private Services.HotkeyService? _hotkeys;
  private Services.SelectionFlowController? _flow;
  private Services.ClipboardHistoryService? _clipboardHistory;
  private Services.PasteHistoryController? _pasteHistory;

  protected override void OnStartup(StartupEventArgs e)
  {
    base.OnStartup(e);

    ShutdownMode = ShutdownMode.OnExplicitShutdown;

    _settings = new Services.SettingsService();
    _settings.Load();

    _clipboardHistory = new Services.ClipboardHistoryService();
    _pasteHistory = new Services.PasteHistoryController(_settings, _clipboardHistory);

    _flow = new Services.SelectionFlowController(_settings, ApplyHotkey, ApplyPasteHistoryHotkey);

    _tray = new Services.TrayService(_settings);
    _tray.StartSelectionRequested += (_, _) => _flow.StartSelection();
    _tray.ShowPasteHistoryRequested += (_, _) => _pasteHistory.ShowOrClose();
    _tray.ExitRequested += (_, _) => Shutdown();
    _tray.ShowSettingsRequested += (_, _) => _flow.ShowSettings();
    _tray.ToggleAutoStartRequested += (_, _) => _tray.ToggleAutoStart();
    _tray.Initialize();

    _hotkeys = new Services.HotkeyService();
    _hotkeys.HotkeyPressedById += (_, id) =>
    {
      if (id == Services.HotkeyService.DefaultHotkeyId)
        _flow.StartSelection();
      else if (id == PasteHistoryHotkeyId)
        _pasteHistory.ShowOrClose();
    };
    TryRegisterStartupHotkeys();
  }

  protected override void OnExit(ExitEventArgs e)
  {
    _hotkeys?.Dispose();
    _tray?.Dispose();
    _pasteHistory?.Dispose();
    _clipboardHistory?.Dispose();
    base.OnExit(e);
  }

  private void TryRegisterStartupHotkeys()
  {
    var hotkey = _settings?.Settings.Hotkey;
    try
    {
      _hotkeys?.RegisterHotkey(hotkey, Services.HotkeyService.DefaultHotkeyId);
    }
    catch (Exception ex)
    {
      try { _hotkeys?.RegisterHotkey("Ctrl+Alt+T", Services.HotkeyService.DefaultHotkeyId); } catch { }
      System.Windows.MessageBox.Show($"Hotkey registration failed: {ex.Message}", "ScreenTranslator");
    }

    var pasteHotkey = _settings?.Settings.PasteHistoryHotkey;
    try
    {
      _hotkeys?.RegisterHotkey(pasteHotkey, PasteHistoryHotkeyId);
    }
    catch (Exception ex)
    {
      try { _hotkeys?.RegisterHotkey("Ctrl+Shift+V", PasteHistoryHotkeyId); } catch { }
      System.Windows.MessageBox.Show($"Paste history hotkey registration failed: {ex.Message}", "ScreenTranslator");
    }
  }

  private string? ApplyHotkey(string hotkey)
  {
    if (_hotkeys is null)
      return "Hotkey service is not initialized.";

    try
    {
      _hotkeys.RegisterHotkey(hotkey, Services.HotkeyService.DefaultHotkeyId);
      return null;
    }
    catch (Exception ex)
    {
      return ex.Message;
    }
  }

  private string? ApplyPasteHistoryHotkey(string hotkey)
  {
    if (_hotkeys is null)
      return "Hotkey service is not initialized.";

    try
    {
      _hotkeys.RegisterHotkey(hotkey, PasteHistoryHotkeyId);
      return null;
    }
    catch (Exception ex)
    {
      return ex.Message;
    }
  }
}

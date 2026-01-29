using System.IO;
using System.Windows;
using System.Windows.Forms;

namespace ScreenTranslator.Services;

public sealed class TrayService : IDisposable
{
  private readonly SettingsService _settings;
  private readonly AutoStartService _autoStart = new();
  private NotifyIcon? _icon;
  private System.Drawing.Icon? _trayIcon;
  private bool _trayIconOwned;

  public event EventHandler? StartSelectionRequested;
  public event EventHandler? ShowPasteHistoryRequested;
  public event EventHandler? ShowSettingsRequested;
  public event EventHandler? ToggleAutoStartRequested;
  public event EventHandler? ExitRequested;

  public TrayService(SettingsService settings)
  {
    _settings = settings;
  }

  public void Initialize()
  {
    _icon = new NotifyIcon
    {
      Visible = true,
      Text = "ScreenTranslator",
      Icon = GetTrayIcon(),
      ContextMenuStrip = BuildMenu(),
    };

    _icon.DoubleClick += (_, _) => StartSelectionRequested?.Invoke(this, EventArgs.Empty);
  }

  private ContextMenuStrip BuildMenu()
  {
    var menu = new ContextMenuStrip();

    var start = new ToolStripMenuItem("Start Selection")
    {
      ShowShortcutKeys = false,
    };
    start.Click += (_, _) => StartSelectionRequested?.Invoke(this, EventArgs.Empty);

    var pasteHistory = new ToolStripMenuItem("历史粘贴")
    {
      ShowShortcutKeys = false,
    };
    pasteHistory.Click += (_, _) => ShowPasteHistoryRequested?.Invoke(this, EventArgs.Empty);

    var settings = new ToolStripMenuItem("Settings");
    settings.Click += (_, _) => ShowSettingsRequested?.Invoke(this, EventArgs.Empty);

    var autoStart = new ToolStripMenuItem("Start with Windows")
    {
      Checked = _autoStart.IsEnabled(),
      CheckOnClick = false,
    };
    autoStart.Click += (_, _) => ToggleAutoStartRequested?.Invoke(this, EventArgs.Empty);
    menu.Opening += (_, _) =>
    {
      autoStart.Checked = _autoStart.IsEnabled();

      var startHotkey = string.IsNullOrWhiteSpace(_settings.Settings.Hotkey) ? "Ctrl+Alt+T" : _settings.Settings.Hotkey.Trim();
      start.Text = $"Start Selection\t{startHotkey}";

      var pasteHotkey = string.IsNullOrWhiteSpace(_settings.Settings.PasteHistoryHotkey)
        ? "Ctrl+Shift+V"
        : _settings.Settings.PasteHistoryHotkey.Trim();
      pasteHistory.Text = $"历史粘贴\t{pasteHotkey}";
    };

    var exit = new ToolStripMenuItem("Exit");
    exit.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);

    menu.Items.Add(start);
    menu.Items.Add(pasteHistory);
    menu.Items.Add(new ToolStripSeparator());
    menu.Items.Add(settings);
    menu.Items.Add(autoStart);
    menu.Items.Add(new ToolStripSeparator());
    menu.Items.Add(exit);
    return menu;
  }

  public void ToggleAutoStart()
  {
    try
    {
      var exePath = Environment.ProcessPath;
      if (string.IsNullOrWhiteSpace(exePath))
        return;

      if (_autoStart.IsEnabled())
        _autoStart.Disable();
      else
        _autoStart.Enable(exePath);
    }
    catch (Exception ex)
    {
      System.Windows.MessageBox.Show($"Failed to update auto-start: {ex.Message}", "ScreenTranslator");
    }
  }

  public void Dispose()
  {
    if (_icon is not null)
    {
      _icon.Visible = false;
      _icon.Dispose();
      _icon = null;
    }

    if (_trayIconOwned)
    {
      _trayIcon?.Dispose();
    }

    _trayIcon = null;
    _trayIconOwned = false;
  }

  private System.Drawing.Icon GetTrayIcon()
  {
    if (_trayIcon is not null)
      return _trayIcon;

    // Try multiple possible locations for the icon
    var possiblePaths = new[]
    {
      Path.Combine(AppContext.BaseDirectory, "Assets", "tray.ico"),
      Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "tray.ico"),
      Path.Combine(Environment.CurrentDirectory, "Assets", "tray.ico"),
      Path.Combine(Path.GetDirectoryName(Environment.ProcessPath ?? "") ?? "", "Assets", "tray.ico"),
    };

    foreach (var assetIcon in possiblePaths)
    {
      if (File.Exists(assetIcon))
      {
        try
        {
          _trayIcon = new System.Drawing.Icon(assetIcon);
          _trayIconOwned = true;
          return _trayIcon;
        }
        catch
        {
          // ignore and try next
        }
      }
    }

    var exePath = Environment.ProcessPath;
    if (!string.IsNullOrWhiteSpace(exePath))
    {
      try
      {
        var icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
        if (icon is not null)
        {
          _trayIconOwned = true;
          _trayIcon = (System.Drawing.Icon)icon.Clone();
          return _trayIcon;
        }
      }
      catch
      {
        // ignore and fall back
      }
    }

    _trayIconOwned = false;
    _trayIcon = System.Drawing.SystemIcons.Application;
    return _trayIcon;
  }
}

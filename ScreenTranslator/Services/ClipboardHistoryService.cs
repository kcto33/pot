using System.Runtime.InteropServices;
using System.Windows.Interop;

using ScreenTranslator.Interop;

using WpfApplication = System.Windows.Application;
using WpfClipboard = System.Windows.Clipboard;
using WpfTextDataFormat = System.Windows.TextDataFormat;

namespace ScreenTranslator.Services;

public sealed class ClipboardHistoryService : IDisposable
{
  private const int MaxItems = 3;
  private const int MaxTextLength = 100_000;

  private readonly object _lock = new();
  private readonly List<string> _recent = new();
  private HwndSource? _source;
  private string? _suppressTextOnce;
  private bool _disposed;

  public event EventHandler? HistoryChanged;

  public ClipboardHistoryService()
  {
    InitializeListenerWindow();
  }

  public IReadOnlyList<string> GetRecent()
  {
    lock (_lock)
    {
      return _recent.ToArray();
    }
  }

  public async Task SetClipboardTextAsync(string text, CancellationToken ct = default)
  {
    ct.ThrowIfCancellationRequested();

    text ??= string.Empty;
    if (text.Length > MaxTextLength)
      text = text[..MaxTextLength];

    _suppressTextOnce = text;

    for (var attempt = 0; attempt < 3; attempt++)
    {
      ct.ThrowIfCancellationRequested();
      try
      {
        WpfClipboard.SetText(text, WpfTextDataFormat.UnicodeText);
        return;
      }
      catch (Exception ex) when (IsClipboardBusy(ex))
      {
        await Task.Delay(20 * (attempt + 1), ct);
      }
    }
  }

  public void Dispose()
  {
    if (_disposed)
      return;
    _disposed = true;

    if (_source is not null)
    {
      try { NativeMethods.RemoveClipboardFormatListener(_source.Handle); } catch { }
      try { _source.RemoveHook(WndProc); } catch { }
      try { _source.Dispose(); } catch { }
      _source = null;
    }
  }

  private void InitializeListenerWindow()
  {
    var p = new HwndSourceParameters("ScreenTranslator.ClipboardListener")
    {
      Width = 0,
      Height = 0,
      ParentWindow = NativeMethods.HWND_MESSAGE,
      WindowStyle = 0,
    };

    _source = new HwndSource(p);
    _source.AddHook(WndProc);

    NativeMethods.AddClipboardFormatListener(_source.Handle);
  }

  private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
  {
    if (msg == NativeMethods.WM_CLIPBOARDUPDATE)
    {
      handled = true;

      _ = WpfApplication.Current.Dispatcher.BeginInvoke(async () =>
      {
        var text = await TryGetClipboardTextAsync();
        if (string.IsNullOrWhiteSpace(text))
          return;

        if (text.Length > MaxTextLength)
          text = text[..MaxTextLength];

        if (_suppressTextOnce is not null && string.Equals(text, _suppressTextOnce, StringComparison.Ordinal))
        {
          _suppressTextOnce = null;
          return;
        }

        AddToHistory(text);
      });
    }

    return IntPtr.Zero;
  }

  private void AddToHistory(string text)
  {
    var changed = false;
    lock (_lock)
    {
      if (_recent.Count > 0 && string.Equals(_recent[0], text, StringComparison.Ordinal))
        return;

      _recent.RemoveAll(s => string.Equals(s, text, StringComparison.Ordinal));
      _recent.Insert(0, text);
      if (_recent.Count > MaxItems)
        _recent.RemoveRange(MaxItems, _recent.Count - MaxItems);

      changed = true;
    }

    if (changed)
      HistoryChanged?.Invoke(this, EventArgs.Empty);
  }

  private static async Task<string?> TryGetClipboardTextAsync()
  {
    for (var attempt = 0; attempt < 3; attempt++)
    {
      try
      {
        if (!WpfClipboard.ContainsText(WpfTextDataFormat.UnicodeText))
          return null;

        return WpfClipboard.GetText(WpfTextDataFormat.UnicodeText);
      }
      catch (Exception ex) when (IsClipboardBusy(ex))
      {
        await Task.Delay(20 * (attempt + 1));
      }
    }

    return null;
  }

  private static bool IsClipboardBusy(Exception ex) =>
    ex is COMException || ex is ExternalException;
}

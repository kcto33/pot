using System.Runtime.InteropServices;

namespace ScreenTranslator.Interop;

internal static class NativeMethods
{
  internal const int WH_MOUSE_LL = 14;
  internal const int WH_KEYBOARD_LL = 13;
  internal const int WM_LBUTTONDOWN = 0x0201;
  internal const int WM_RBUTTONDOWN = 0x0204;

  internal const int WM_KEYDOWN = 0x0100;
  internal const int WM_SYSKEYDOWN = 0x0104;
  internal const int WM_CLIPBOARDUPDATE = 0x031D;

  internal const int GWL_EXSTYLE = -20;
  internal const int WS_EX_TOOLWINDOW = 0x00000080;
  internal const int WS_EX_NOACTIVATE = 0x08000000;

  internal const uint SWP_NOACTIVATE = 0x0010;
  internal const uint SWP_NOSIZE = 0x0001;
  internal const uint SWP_NOMOVE = 0x0002;
  internal const uint SWP_NOZORDER = 0x0004;
  internal const uint SWP_SHOWWINDOW = 0x0040;

  internal static readonly IntPtr HWND_MESSAGE = new(-3);

  [DllImport("user32.dll")]
  internal static extern bool GetCursorPos(out POINT lpPoint);

  [DllImport("user32.dll", SetLastError = true)]
  internal static extern bool AddClipboardFormatListener(IntPtr hwnd);

  [DllImport("user32.dll", SetLastError = true)]
  internal static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

  [DllImport("user32.dll", SetLastError = true)]
  internal static extern int GetWindowLong(IntPtr hWnd, int nIndex);

  [DllImport("user32.dll", SetLastError = true)]
  internal static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

  [DllImport("user32.dll", SetLastError = true)]
  internal static extern bool SetWindowPos(
    IntPtr hWnd,
    IntPtr hWndInsertAfter,
    int X,
    int Y,
    int cx,
    int cy,
    uint uFlags);

  [DllImport("user32.dll", SetLastError = true)]
  internal static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

  internal delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

  internal delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

  [DllImport("user32.dll", SetLastError = true)]
  internal static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

  [DllImport("user32.dll", SetLastError = true)]
  internal static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

  [DllImport("user32.dll", SetLastError = true)]
  internal static extern bool UnhookWindowsHookEx(IntPtr hhk);

  [DllImport("user32.dll", SetLastError = true)]
  internal static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

  internal const int INPUT_KEYBOARD = 1;
  internal const uint KEYEVENTF_KEYUP = 0x0002;

  [DllImport("user32.dll", SetLastError = true)]
  internal static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

  [StructLayout(LayoutKind.Sequential)]
  internal struct POINT
  {
    public int X;
    public int Y;
  }

  [StructLayout(LayoutKind.Sequential)]
  internal struct RECT
  {
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
  }

  [StructLayout(LayoutKind.Sequential)]
  internal struct MSLLHOOKSTRUCT
  {
    public POINT pt;
    public uint mouseData;
    public uint flags;
    public uint time;
    public IntPtr dwExtraInfo;
  }

  [StructLayout(LayoutKind.Sequential)]
  internal struct KBDLLHOOKSTRUCT
  {
    public uint vkCode;
    public uint scanCode;
    public uint flags;
    public uint time;
    public IntPtr dwExtraInfo;
  }

  [StructLayout(LayoutKind.Sequential)]
  internal struct INPUT
  {
    public uint type;
    public INPUTUNION u;
  }

  [StructLayout(LayoutKind.Explicit)]
  internal struct INPUTUNION
  {
    [FieldOffset(0)]
    public KEYBDINPUT ki;
    [FieldOffset(0)]
    public MOUSEINPUT mi;
  }

  [StructLayout(LayoutKind.Sequential)]
  internal struct MOUSEINPUT
  {
    public int dx;
    public int dy;
    public uint mouseData;
    public uint dwFlags;
    public uint time;
    public IntPtr dwExtraInfo;
  }

  [StructLayout(LayoutKind.Sequential)]
  internal struct KEYBDINPUT
  {
    public ushort wVk;
    public ushort wScan;
    public uint dwFlags;
    public uint time;
    public IntPtr dwExtraInfo;
  }
}

using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace ScreenSnap.Tray;

/// <summary>
/// A taskbar notification ("tray") icon backed by a hidden message-only window.
/// Uses <c>Shell_NotifyIcon</c> + a native popup menu via CsWin32-generated P/Invoke,
/// keeping third-party dependencies out of the project. All events are raised on the
/// UI thread (the thread that constructs the icon and pumps the message loop).
/// </summary>
internal sealed class TrayIcon : IDisposable
{
    private const string WindowClassName = "ScreenSnapTrayWindow";
    private const uint TrayIconId = 1;
    private const uint TrayCallbackMessage = 0x8000 + 1; // WM_APP + 1

    // Menu command identifiers (presets are offset from a base so they never collide).
    private const int CmdSettings = 1;
    private const int CmdExit = 2;
    private const int CmdPresetBase = 0x1000;

    // Well-known window messages (stable values; avoids pulling extra metadata).
    private const uint WM_DESTROY = 0x0002;
    private const uint WM_LBUTTONUP = 0x0202;
    private const uint WM_LBUTTONDBLCLK = 0x0203;
    private const uint WM_RBUTTONUP = 0x0205;
    private const uint WM_CONTEXTMENU = 0x007B;
    private const uint WM_HOTKEY = 0x0312;

    // TrackPopupMenuEx flags (uFlags is a plain uint in the projection).
    private const uint TPM_RIGHTBUTTON = 0x0002;
    private const uint TPM_NONOTIFY = 0x0080;
    private const uint TPM_RETURNCMD = 0x0100;

    private readonly WNDPROC _wndProc; // kept rooted so the native callback stays valid
    private readonly HINSTANCE _hinstance;
    private readonly uint _taskbarCreatedMessage;

    private HWND _hwnd;
    private HICON _icon;
    private ushort _classAtom;
    private IReadOnlyList<string> _presetNames = Array.Empty<string>();
    private bool _iconAdded;
    private bool _disposed;

    public TrayIcon()
    {
        _wndProc = WndProc;
        _hinstance = new HINSTANCE(Marshal.GetHINSTANCE(typeof(TrayIcon).Module));
        _taskbarCreatedMessage = PInvoke.RegisterWindowMessage("TaskbarCreated");

        CreateMessageWindow();
        _icon = LoadTrayIcon();
        AddIcon();
    }

    /// <summary>Raised when the user picks a preset from the menu (argument is the preset index).</summary>
    public event Action<int>? PresetSelected;

    /// <summary>Raised when the user picks "Settings".</summary>
    public event Action? SettingsRequested;

    /// <summary>Raised when the user picks "Exit".</summary>
    public event Action? ExitRequested;

    /// <summary>Raised when a registered global hotkey fires (argument is the hotkey id).</summary>
    public event Action<int>? HotkeyPressed;

    /// <summary>The hidden window's handle; used to host <c>RegisterHotKey</c>.</summary>
    public HWND WindowHandle => _hwnd;

    /// <summary>Updates the preset names shown in the context menu.</summary>
    public void SetPresets(IReadOnlyList<string> names)
    {
        _presetNames = names ?? Array.Empty<string>();
    }

    /// <summary>Shows a balloon/toast notification from the tray icon.</summary>
    public unsafe void ShowBalloon(string title, string message, bool isError = false)
    {
        if (!_iconAdded)
            return;

        var data = new NOTIFYICONDATAW
        {
            cbSize = (uint)sizeof(NOTIFYICONDATAW),
            hWnd = _hwnd,
            uID = TrayIconId,
            uFlags = NOTIFY_ICON_DATA_FLAGS.NIF_INFO,
            dwInfoFlags = isError ? NOTIFY_ICON_INFOTIP_FLAGS.NIIF_ERROR : NOTIFY_ICON_INFOTIP_FLAGS.NIIF_INFO,
        };
        data.szInfoTitle = title;
        data.szInfo = message;

        PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_MODIFY, in data);
    }

    private unsafe void CreateMessageWindow()
    {
        fixed (char* classNamePtr = WindowClassName)
        {
            var wc = new WNDCLASSEXW
            {
                cbSize = (uint)sizeof(WNDCLASSEXW),
                lpfnWndProc = _wndProc,
                hInstance = _hinstance,
                lpszClassName = new PCWSTR(classNamePtr),
            };

            _classAtom = PInvoke.RegisterClassEx(in wc);
            if (_classAtom == 0)
                throw new InvalidOperationException("RegisterClassEx failed for the tray window.");

            _hwnd = PInvoke.CreateWindowEx(
                (WINDOW_EX_STYLE)0,
                new PCWSTR(classNamePtr),
                default,
                (WINDOW_STYLE)0,
                0, 0, 0, 0,
                HWND.Null,
                (HMENU)default,
                _hinstance,
                null);
        }

        if (_hwnd.IsNull)
            throw new InvalidOperationException("CreateWindowEx failed for the tray window.");
    }

    private unsafe HICON LoadTrayIcon()
    {
        // Prefer the bundled app icon, sized for the notification area (DPI-aware).
        int cx = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSMICON);
        int cy = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYSMICON);
        if (cx <= 0) cx = 16;
        if (cy <= 0) cy = 16;

        string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "ScreenSnap.ico");
        if (File.Exists(iconPath))
        {
            fixed (char* pathPtr = iconPath)
            {
                HANDLE handle = PInvoke.LoadImage(
                    default,
                    new PCWSTR(pathPtr),
                    GDI_IMAGE_TYPE.IMAGE_ICON,
                    cx,
                    cy,
                    IMAGE_FLAGS.LR_LOADFROMFILE);

                if (!handle.IsNull)
                    return (HICON)handle.Value;
            }
        }

        // Fallback: a stock "desktop PC" icon so the tray always has something to show.
        var info = new SHSTOCKICONINFO { cbSize = (uint)sizeof(SHSTOCKICONINFO) };
        HRESULT hr = PInvoke.SHGetStockIconInfo(
            SHSTOCKICONID.SIID_DESKTOPPC,
            SHGSI_FLAGS.SHGSI_ICON | SHGSI_FLAGS.SHGSI_SMALLICON,
            ref info);

        return hr.Succeeded ? info.hIcon : default;
    }

    private unsafe void AddIcon()
    {
        var data = new NOTIFYICONDATAW
        {
            cbSize = (uint)sizeof(NOTIFYICONDATAW),
            hWnd = _hwnd,
            uID = TrayIconId,
            uFlags = NOTIFY_ICON_DATA_FLAGS.NIF_MESSAGE | NOTIFY_ICON_DATA_FLAGS.NIF_ICON | NOTIFY_ICON_DATA_FLAGS.NIF_TIP,
            uCallbackMessage = TrayCallbackMessage,
            hIcon = _icon,
        };
        data.szTip = "ScreenSnap";

        _iconAdded = PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_ADD, in data);
    }

    private unsafe void RemoveIcon()
    {
        if (!_iconAdded)
            return;

        var data = new NOTIFYICONDATAW
        {
            cbSize = (uint)sizeof(NOTIFYICONDATAW),
            hWnd = _hwnd,
            uID = TrayIconId,
        };
        PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_DELETE, in data);
        _iconAdded = false;
    }

    private LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        if (msg == TrayCallbackMessage)
        {
            uint mouseMessage = (uint)(lParam.Value & 0xFFFF);
            switch (mouseMessage)
            {
                case WM_LBUTTONUP:
                case WM_LBUTTONDBLCLK:
                    // Left-click (or double-click) opens the app; only right-click shows the menu.
                    SettingsRequested?.Invoke();
                    return default;
                case WM_RBUTTONUP:
                case WM_CONTEXTMENU:
                    ShowContextMenu();
                    return default;
            }

            return default;
        }

        if (msg == WM_HOTKEY)
        {
            HotkeyPressed?.Invoke((int)wParam.Value);
            return default;
        }

        if (msg == _taskbarCreatedMessage)
        {
            // Explorer restarted and dropped our icon; re-add it.
            AddIcon();
            return default;
        }

        return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
    }

    private unsafe void ShowContextMenu()
    {
        HMENU menuHandle = PInvoke.CreatePopupMenu();
        if (menuHandle.IsNull)
            return;

        using var menu = new DestroyMenuSafeHandle(menuHandle, ownsHandle: true);

        var names = _presetNames;
        if (names.Count == 0)
        {
            PInvoke.AppendMenu(menu, MENU_ITEM_FLAGS.MF_STRING | MENU_ITEM_FLAGS.MF_GRAYED, 0, "(No presets)");
        }
        else
        {
            for (int i = 0; i < names.Count; i++)
                PInvoke.AppendMenu(menu, MENU_ITEM_FLAGS.MF_STRING, (nuint)(CmdPresetBase + i), names[i]);
        }

        PInvoke.AppendMenu(menu, MENU_ITEM_FLAGS.MF_SEPARATOR, 0, default(string));
        PInvoke.AppendMenu(menu, MENU_ITEM_FLAGS.MF_STRING, (nuint)CmdSettings, "Settings\u2026");
        PInvoke.AppendMenu(menu, MENU_ITEM_FLAGS.MF_STRING, (nuint)CmdExit, "Exit");

        PInvoke.GetCursorPos(out System.Drawing.Point cursor);

        // Required so the menu dismisses correctly when the user clicks elsewhere.
        PInvoke.SetForegroundWindow(_hwnd);

        BOOL result = PInvoke.TrackPopupMenuEx(
            menu,
            TPM_RIGHTBUTTON | TPM_RETURNCMD | TPM_NONOTIFY,
            cursor.X,
            cursor.Y,
            _hwnd,
            null);

        int command = result.Value;
        if (command == 0)
            return;

        if (command == CmdSettings)
            SettingsRequested?.Invoke();
        else if (command == CmdExit)
            ExitRequested?.Invoke();
        else if (command >= CmdPresetBase)
            PresetSelected?.Invoke(command - CmdPresetBase);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        RemoveIcon();

        if (!_hwnd.IsNull)
        {
            PInvoke.DestroyWindow(_hwnd);
            _hwnd = default;
        }

        if (!_icon.IsNull)
        {
            PInvoke.DestroyIcon(_icon);
            _icon = default;
        }
    }
}

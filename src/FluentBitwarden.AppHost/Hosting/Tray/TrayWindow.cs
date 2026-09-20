namespace FluentBitwarden.AppHost.Hosting.Tray;

internal sealed class TrayWindow : IDisposable
{
    private const uint CloseMessage = 0x0010;
    private const uint DestroyMessage = 0x0002;
    private const uint AppMessage = 0x8000;
    private const uint TrayCallbackMessage = AppMessage + 1;

    private static TrayWindow? _current;

    private readonly HINSTANCE _moduleHandle;
    private readonly HWND _windowHandle;
    private readonly NotificationIcon _trayIcon;
    private readonly string _windowName;
    private readonly ITrayCommandsHandler _commandsHandler;
    private bool _windowDestroyed;
    private bool _disposed;

    public TrayWindow(string windowName, ITrayCommandsHandler commandsHandler)
    {
        _windowName = windowName;
        _commandsHandler = commandsHandler;
        _moduleHandle = PInvoke.GetModuleHandle(default(PCWSTR));
        RegisterWindowClass();

        _current = this;
        _windowHandle = CreateHiddenWindow();
        _trayIcon = NotificationIcon.Create(_windowHandle, TrayCallbackMessage);
    }

    public void Run()
    {
        while (PInvoke.GetMessage(out var message, default, 0, 0).Value > 0)
        {
            PInvoke.TranslateMessage(message);
            PInvoke.DispatchMessage(message);
        }
    }

    public void RequestShutdown()
    {
        if (!PInvoke.PostMessage(_windowHandle, CloseMessage, default, default))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not post WM_CLOSE to the tray window.");
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _trayIcon.Dispose();

        if (!_windowDestroyed)
            PInvoke.DestroyWindow(_windowHandle);

        _current = null;
    }

    private unsafe void RegisterWindowClass()
    {
        fixed (char* windowClassName = _windowName)
        {
            var windowClass = new WNDCLASSEXW
            {
                cbSize = (uint)sizeof(WNDCLASSEXW),
                lpfnWndProc = &WndProc,
                hInstance = _moduleHandle,
                lpszClassName = windowClassName
            };

            if (PInvoke.RegisterClassEx(in windowClass) == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not register the AppHost window class.");
        }
    }

    private unsafe HWND CreateHiddenWindow()
    {
        fixed (char* windowNamePtr = _windowName)
        {
            HWND windowHandle = PInvoke.CreateWindowEx(
                default,
                windowNamePtr,
                windowNamePtr,
                WINDOW_STYLE.WS_OVERLAPPED,
                0,
                0,
                0,
                0,
                default,
                default,
                _moduleHandle,
                null);

            return !windowHandle.IsNull
                ? windowHandle
                : throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not create the AppHost window.");
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static LRESULT WndProc(HWND windowHandle, uint message, WPARAM wParam, LPARAM lParam)
    {
        switch (message)
        {
            case TrayCallbackMessage:
                _current?.HandleTrayCallback((TrayIconMessage)unchecked((uint)(nint)lParam));
                return default;

            case CloseMessage:
                PInvoke.DestroyWindow(windowHandle);
                return default;

            case DestroyMessage:
                _current?.HandleWindowDestroyed();
                PInvoke.PostQuitMessage(0);
                return default;

            default:
                return PInvoke.DefWindowProc(windowHandle, message, wParam, lParam);
        }
    }

    private void HandleWindowDestroyed()
    {
        _windowDestroyed = true;
        _trayIcon.Dispose();
    }

    private void HandleTrayCallback(TrayIconMessage message)
    {
        switch (message)
        {
            case TrayIconMessage.LeftButtonDown:
            case TrayIconMessage.LeftButtonDoubleClick:
            case TrayIconMessage.Select:
            case TrayIconMessage.KeySelect:
                _commandsHandler.HandleLeftClick();
                return;

            case TrayIconMessage.ContextMenu:
            case TrayIconMessage.RightButtonUp:
                _commandsHandler.HandleRightClick(TrayMenu.Show(_windowHandle));
                return;
        }
    }
}

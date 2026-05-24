using FluentBitwarden.AppHost.Infrastructure.Activation;

namespace FluentBitwarden.AppHost.Application.Tray;

internal sealed class TrayHost
{
    private const uint DestroyMessage = 0x0002;
    private const uint AppMessage = 0x8000;
    private const uint TrayCallbackMessage = AppMessage + 1;

    private const string WindowName = "FluentBitwarden.AppHost";

    private static TrayHost? _current;

    private readonly HINSTANCE _moduleHandle;
    private readonly HWND _windowHandle;

    public TrayHost()
    {
        _moduleHandle = PInvoke.GetModuleHandle(default(PCWSTR));
        RegisterWindowClass();

        _current = this;
        _windowHandle = CreateHiddenWindow();
        TrayIcon.CreateNotifyIcon(_windowHandle, TrayCallbackMessage);
    }

    public void RequestShutdown() => PInvoke.DestroyWindow(_windowHandle);

    private unsafe void RegisterWindowClass()
    {
        fixed (char* windowClassName = WindowName)
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
        fixed (char* windowNamePtr = WindowName)
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
                _current!.HandleTrayCallback((TrayIconMessage)unchecked((uint)(nint)lParam));
                return default;

            case DestroyMessage:
                PInvoke.PostQuitMessage(0);
                return default;

            default:
                return PInvoke.DefWindowProc(windowHandle, message, wParam, lParam);
        }
    }

    private void HandleTrayCallback(TrayIconMessage callbackMessage)
    {
        switch (callbackMessage)
        {
            case TrayIconMessage.LeftButtonDown:
            case TrayIconMessage.LeftButtonDoubleClick:
            case TrayIconMessage.Select:
            case TrayIconMessage.KeySelect:
                AppProcessLauncher.Activate();
                return;

            case TrayIconMessage.ContextMenu:
            case TrayIconMessage.RightButtonUp:
                TrayMenu.Show(_windowHandle);
                return;
        }
    }
}

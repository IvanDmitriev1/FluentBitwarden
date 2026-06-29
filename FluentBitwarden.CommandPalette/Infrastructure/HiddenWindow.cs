using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace FluentBitwarden.CommandPalette.Infrastructure;

internal sealed unsafe partial class HiddenWindow : IDisposable
{
    public HiddenWindow(string title)
    {
        _title = title;
        _className = $"{title}_{Guid.NewGuid():N}";
        _hInstance = PInvoke.GetModuleHandle((PCWSTR)null);

        RegisterWindowClass();
        CreateHiddenWindow();
    }

    private readonly string _title;
    private readonly string _className;
    private readonly HINSTANCE _hInstance;
    private HWND _hwnd;
    private bool _disposed;

    public IntPtr Hwnd
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            return _hwnd.IsNull
                ? throw new InvalidOperationException("Hidden Windows Hello owner window was not created.")
                : _hwnd;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_hwnd != HWND.Null)
        {
            PInvoke.DestroyWindow(_hwnd);
            _hwnd = HWND.Null;
        }

        fixed (char* classNamePtr = _className)
        {
            PInvoke.UnregisterClass(classNamePtr, _hInstance);
        }

        _disposed = true;
    }

    private void RegisterWindowClass()
    {
        fixed (char* classNamePtr = _className)
        {
            var wc = new WNDCLASSEXW
            {
                cbSize = (uint)sizeof(WNDCLASSEXW),
                lpfnWndProc = &WndProc,
                hInstance = _hInstance,
                lpszClassName = classNamePtr,
            };

            ushort atom = PInvoke.RegisterClassEx(wc);

            if (atom == 0)
            {
                throw new InvalidOperationException(
                    $"Failed to register hidden owner window class. Win32Error={Marshal.GetLastWin32Error()}");
            }
        }
    }

    private void CreateHiddenWindow()
    {
        fixed (char* classNamePtr = _className)
        fixed (char* titlePtr = _title)
        {
            _hwnd = PInvoke.CreateWindowEx(
                WINDOW_EX_STYLE.WS_EX_TOOLWINDOW | WINDOW_EX_STYLE.WS_EX_NOACTIVATE, classNamePtr,
                titlePtr,
                WINDOW_STYLE.WS_OVERLAPPED,
                -32000,
                -32000,
                1,
                1,
                hInstance: _hInstance
            );

            if (_hwnd.IsNull)
            {
                throw new InvalidOperationException(
                    $"Failed to create hidden Windows Hello owner window. Win32Error={Marshal.GetLastWin32Error()}");
            }
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    internal static LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
    }
}

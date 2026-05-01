using Microsoft.UI.Xaml;
using Windows.Win32;
using Windows.Win32.Foundation;
using WinUIEx;

namespace FluentBitwarden.Shared.Extensions;

public static class WindowExtensions
{
    private const nuint SubclassId = 0x1234_5678;
    private const uint WM_NCLBUTTONDBLCLK = 0x00A3;

    public static unsafe void PreventMaximizeOnTitleBarDoubleClick(this Window window)
    {
        var hwnd = new HWND(window.GetWindowHandle());

        PInvoke.SetWindowSubclass(
            hwnd,
            &WindowSubclassProc,
            SubclassId,
            0);
    }

    [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvStdcall)])]
    private static LRESULT WindowSubclassProc(
        HWND hwnd,
        uint message,
        WPARAM wParam,
        LPARAM lParam,
        nuint subclassId,
        nuint refData)
    {
        if (message == WM_NCLBUTTONDBLCLK)
        {
            return new LRESULT(0);
        }

        return PInvoke.DefSubclassProc(
            hwnd,
            message,
            wParam,
            lParam);
    }
}
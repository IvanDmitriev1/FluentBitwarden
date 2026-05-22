using FluentBitwarden.AppHost.Infrastructure.Activation;

namespace FluentBitwarden.AppHost.App;

internal static class TrayMenu
{
    private const uint ShowCommand = 1;
    private const uint LockCommand = 2;
    private const uint ExitCommand = 3;

    public static void Show(HWND windowHandle)
    {
        if (!PInvoke.GetCursorPos(out var cursor))
            return;

        PInvoke.SetForegroundWindow(windowHandle);
        using var popupMenuHandle = PInvoke.CreatePopupMenu_SafeHandle();
        if (popupMenuHandle.IsInvalid)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not create the tray menu.");

        PInvoke.AppendMenu(popupMenuHandle, MENU_ITEM_FLAGS.MF_STRING, ShowCommand, "Show");
        PInvoke.AppendMenu(popupMenuHandle, MENU_ITEM_FLAGS.MF_STRING, LockCommand, "Lock");
        PInvoke.AppendMenu(popupMenuHandle, MENU_ITEM_FLAGS.MF_SEPARATOR, 0, string.Empty);
        PInvoke.AppendMenu(popupMenuHandle, MENU_ITEM_FLAGS.MF_STRING, ExitCommand, "Exit");

        BOOL selectedCommand = PInvoke.TrackPopupMenu(
            popupMenuHandle,
            TRACK_POPUP_MENU_FLAGS.TPM_NONOTIFY |
            TRACK_POPUP_MENU_FLAGS.TPM_RETURNCMD |
            TRACK_POPUP_MENU_FLAGS.TPM_RIGHTBUTTON,
            cursor.X,
            cursor.Y,
            windowHandle);

        uint command = (uint)selectedCommand.Value;
        switch (command)
        {
            case ShowCommand:
                AppProcessLauncher.Activate(AppLifecycleCommand.Show);
                return;

            case LockCommand:
                AppProcessLauncher.Activate(AppLifecycleCommand.Lock);
                return;

            case ExitCommand:
                AppProcessLauncher.Activate(AppLifecycleCommand.Exit);
                PInvoke.DestroyWindow(windowHandle);
                return;
        }
    }
}

namespace FluentBitwarden.AppHost.Application.Tray;

internal static class TrayMenu
{ 
    public static TrayMenuCommand Show(HWND windowHandle)
    {
        if (!PInvoke.GetCursorPos(out var cursor))
            return TrayMenuCommand.None;

        PInvoke.SetForegroundWindow(windowHandle);
        using var popupMenuHandle = PInvoke.CreatePopupMenu_SafeHandle();
        if (popupMenuHandle.IsInvalid)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not create the tray menu.");

        PInvoke.AppendMenu(popupMenuHandle, MENU_ITEM_FLAGS.MF_STRING, (uint)TrayMenuCommand.Show, "Show");
        PInvoke.AppendMenu(popupMenuHandle, MENU_ITEM_FLAGS.MF_STRING, (uint)TrayMenuCommand.Lock, "Lock");
        PInvoke.AppendMenu(popupMenuHandle, MENU_ITEM_FLAGS.MF_SEPARATOR, (uint)TrayMenuCommand.None, string.Empty);
        PInvoke.AppendMenu(popupMenuHandle, MENU_ITEM_FLAGS.MF_STRING, (uint)TrayMenuCommand.Exit, "Exit");

        BOOL selectedCommand = PInvoke.TrackPopupMenu(
            popupMenuHandle,
            TRACK_POPUP_MENU_FLAGS.TPM_NONOTIFY |
            TRACK_POPUP_MENU_FLAGS.TPM_RETURNCMD |
            TRACK_POPUP_MENU_FLAGS.TPM_RIGHTBUTTON,
            cursor.X,
            cursor.Y,
            windowHandle);

        uint command = (uint)selectedCommand.Value;
        return (TrayMenuCommand)command;
    }
}

namespace FluentBitwarden.AppHost.Infrastructure.Tray;

internal static class TrayMenu
{
    public static TrayMenuItem.Command? Show(HWND windowHandle, IReadOnlyList<TrayMenuItem> items)
    {
        if (!PInvoke.GetCursorPos(out var cursor))
            return null;

        PInvoke.SetForegroundWindow(windowHandle);
        using var popupMenuHandle = PInvoke.CreatePopupMenu_SafeHandle();
        if (popupMenuHandle.IsInvalid)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not create the tray menu.");

        for (int index = 0; index < items.Count; index++)
        {
            uint itemId = (uint)index + 1;
            switch (items[index])
            {
                case TrayMenuItem.Command command:
                    PInvoke.AppendMenu(popupMenuHandle, MENU_ITEM_FLAGS.MF_STRING, itemId, command.Text);
                    break;

                case TrayMenuItem.Separator:
                    PInvoke.AppendMenu(popupMenuHandle, MENU_ITEM_FLAGS.MF_SEPARATOR, itemId, string.Empty);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(items));
            }
        }

        BOOL selectedCommand = PInvoke.TrackPopupMenu(
            popupMenuHandle,
            TRACK_POPUP_MENU_FLAGS.TPM_NONOTIFY |
            TRACK_POPUP_MENU_FLAGS.TPM_RETURNCMD |
            TRACK_POPUP_MENU_FLAGS.TPM_RIGHTBUTTON,
            cursor.X,
            cursor.Y,
            windowHandle);

        return GetSelectedCommand(items, (uint)selectedCommand.Value);
    }

    internal static TrayMenuItem.Command? GetSelectedCommand(IReadOnlyList<TrayMenuItem> items, uint itemId)
    {
        if (itemId == 0)
            return null;

        uint itemIndex = itemId - 1;
        return itemIndex < items.Count ? items[(int)itemIndex] as TrayMenuItem.Command : null;
    }
}

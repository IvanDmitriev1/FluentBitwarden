namespace FluentBitwarden.AppHost.Application.Tray;

internal static class TrayIcon
{
    private const uint IconId = 1;

    public static void CreateNotifyIcon(HWND windowHandle, uint callbackMessage)
    {
        var icon = CreateIconImage();
        var data = new NOTIFYICONDATAW
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATAW>(),
            hWnd = windowHandle,
            uID = IconId,
            uFlags = NOTIFY_ICON_DATA_FLAGS.NIF_MESSAGE |
                     NOTIFY_ICON_DATA_FLAGS.NIF_ICON |
                     NOTIFY_ICON_DATA_FLAGS.NIF_TIP,
            uCallbackMessage = callbackMessage,
            hIcon = icon,
            szTip = "FluentBitwarden"
        };

        if (!PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_ADD, in data))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not add the FluentBitwarden tray icon.");
    }

    private static unsafe HICON CreateIconImage()
    {
        string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Bitwarden_icon.ico");

        fixed (char* iconPathPtr = iconPath)
        {
            HANDLE iconHandle = PInvoke.LoadImage(
                default,
                iconPathPtr,
                GDI_IMAGE_TYPE.IMAGE_ICON,
                0,
                0,
                IMAGE_FLAGS.LR_DEFAULTSIZE | IMAGE_FLAGS.LR_LOADFROMFILE);

            if (!iconHandle.IsNull)
                return (HICON)(IntPtr)iconHandle;
        }

        HICON fallbackIcon = PInvoke.LoadIcon(default, new PCWSTR((char*)32512));
        return fallbackIcon;
    }
}

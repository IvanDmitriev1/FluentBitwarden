namespace FluentBitwarden.AppHost.Application.Tray;

internal enum TrayIconMessage : uint
{
    ContextMenu = 0x007B,          // WM_CONTEXTMENU
    RightButtonUp = 0x0205,        // WM_RBUTTONUP
    LeftButtonDown = 0x0201,          // WM_LBUTTONDOWN
    LeftButtonDoubleClick = 0x0203,   // WM_LBUTTONDBLCLK


    Select = 0x0400,               // NIN_SELECT = WM_USER
    KeySelect = 0x0401,            // NIN_KEYSELECT = WM_USER + 1
}
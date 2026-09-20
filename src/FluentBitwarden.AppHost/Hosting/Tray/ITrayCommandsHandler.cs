namespace FluentBitwarden.AppHost.Hosting.Tray;

internal interface ITrayCommandsHandler
{
    void HandleLeftClick();
    void HandleRightClick(TrayMenuCommand command);
}

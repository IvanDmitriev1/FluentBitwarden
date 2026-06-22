namespace FluentBitwarden.AppHost.Infrastructure.Services;

internal interface IUiProcessLauncher
{
    bool IsRunning { get; }

    void ActivateMainWindow();
    void Activate();
    void Exit();
}

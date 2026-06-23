namespace FluentBitwarden.AppHost.Infrastructure.Services;

internal interface IUiProcessLauncher
{
    bool IsRunning { get; }

    event Action ProcessExited;

    void ActivateMainWindow();
    void Activate();
    void Exit();
}



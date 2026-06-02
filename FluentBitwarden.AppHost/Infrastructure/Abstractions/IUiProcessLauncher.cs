namespace FluentBitwarden.AppHost.Infrastructure.Abstractions;

internal interface IUiProcessLauncher
{
    bool IsRunning { get; }

    void ActivateMainWindow();

    void ActivateOverlay();

    void Activate();

    void Exit();
}

using FluentBitwarden.Platform.Infrastructure.ProcessManager;

namespace FluentBitwarden.AppHost.Infrastructure.Services;

internal interface IUiProcessLauncher : IProcessManager
{ 
    void ActivateMainWindow();
}

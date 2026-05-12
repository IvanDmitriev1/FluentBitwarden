using System.Linq;
using BitwardenApi.Models;
using FluentBitwarden.Infrastructure.Services.Abstractions;
using FluentBitwarden.Modules.Vault.Abstractions;
using FluentBitwarden.UI.Controls.Lifecycle;

namespace FluentBitwarden.Views.Shell;

public sealed partial class ShellPageViewModel(
    IConnectivityService connectivityService,
    IVaultService vaultService,
    ISiteIconCache siteIconCache) : ObservableObject, IPageLifecycleAware
{
    public void OnUnloading() { }

    public Task OnLoadingAsync(CancellationToken cancellationToken)
    {
        
        return Task.CompletedTask;
    }
}
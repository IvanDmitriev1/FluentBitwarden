using System.Linq;
using BitwardenApi.Modules.Vault.Models;
using FluentBitwarden.Modules.Vault.Abstractions;
using FluentBitwarden.Resources.Controls.Lifecycle;
using FluentBitwarden.Shared.Services.Abstractions;

namespace FluentBitwarden.Views.Shell;

public sealed partial class ShellPageViewModel(
    IConnectivityService connectivityService,
    IVaultSyncService vaultSyncService,
    ISiteIconCache siteIconCache) : ObservableObject, IPageLifecycleAware
{
    public void OnUnloading() { }

    public Task OnLoadingAsync(CancellationToken cancellationToken)
    {
        if (connectivityService.HasInternetAccess)
        {
            var urls = vaultSyncService.Ciphers
                .OfType<LoginCipher>()
                .Select(static c => c.Uris.FirstOrDefault())
                .Where(static s => !string.IsNullOrWhiteSpace(s))
                .Select(static s => Uri.TryCreate(s, UriKind.Absolute, out var uri) ? uri : null)
                .Where(static uri => uri is not null)
                .Cast<Uri>()
                .ToArray();

            _ = Task.Run(() => siteIconCache.PreloadAsync(urls, cancellationToken));
        }

        return Task.CompletedTask;
    }
}
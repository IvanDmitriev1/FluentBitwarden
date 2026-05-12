using System.Linq;
using BitwardenApi.Models;
using FluentBitwarden.Infrastructure.Services.Abstractions;
using FluentBitwarden.Modules.Vault.Abstractions;
using FluentBitwarden.Resources.Controls.Lifecycle;

namespace FluentBitwarden.Views.Shell;

public sealed partial class ShellPageViewModel(
    IConnectivityService connectivityService,
    IVaultService vaultService,
    ISiteIconCache siteIconCache) : ObservableObject, IPageLifecycleAware
{
    public void OnUnloading() { }

    public Task OnLoadingAsync(CancellationToken cancellationToken)
    {
        /*if (connectivityService.HasInternetAccess)
        {
            var urls = vaultSyncService.Ciphers
                .OfType<LoginVaultCipher>()
                .Select(static c => c.Uris.FirstOrDefault())
                .Where(static s => !string.IsNullOrWhiteSpace(s))
                .Select(static s => Uri.TryCreate(s, UriKind.Absolute, out var uri) ? uri : null)
                .Where(static uri => uri is not null)
                .Cast<Uri>()
                .ToArray();

            _ = Task.Run(() => siteIconCache.PreloadAsync(urls, cancellationToken));
        }

        */

        return Task.CompletedTask;
    }
}
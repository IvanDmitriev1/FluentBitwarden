using FluentBitwarden.Contracts.Modules;
using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;
using FluentBitwarden.Platform.Ipc.Abstractions;
using FluentBitwarden.Platform.Ipc.Transport;
using FluentBitwarden.Platform.SiteIcons;
using Windows.Networking.Connectivity;

namespace FluentBitwarden.Infrastructure.Clients;

[Fody.ConfigureAwait(false)]
internal sealed class RemoteVaultClient(IIpcClient client, ISiteIconCache iconCache) : IVaultClient
{
    public ValueTask<VaultSyncResult> SyncVaultAsync(CancellationToken cancellationToken = default)
    {
        return client.SendAsync<VaultSyncResult>(IpcMessageTypes.Vault.Sync, cancellationToken);
    }

    public async ValueTask<VaultCipher[]> SearchCiphersAsync(VaultCipherQuery query, CancellationToken cancellationToken = default)
    {
        var result = await client.SendAsync<VaultCipherQuery, VaultCipher[]>(query, cancellationToken);
        if (NetworkInformation.HasInternetAccess)
            _ = PreloadSiteIconsAsync(result);

        return result;
    }

    public ValueTask<VaultCipher?> GetCipherAsync(GetVaultCipherRequest request, CancellationToken cancellationToken = default)
    {
        return client.SendAsync<GetVaultCipherRequest, VaultCipher?>(request, cancellationToken);
    }

    public ValueTask<VaultCipher?> SaveCipherAsync(SaveVaultCipherRequest request, CancellationToken cancellationToken = default)
    {
        return client.SendAsync<SaveVaultCipherRequest, VaultCipher?>(request, cancellationToken);
    }

    public async ValueTask DownloadCipherAttachmentAsync(
        DownloadVaultCipherAttachmentRequest request,
        CancellationToken cancellationToken = default)
    {
        _ = await client.SendAsync<DownloadVaultCipherAttachmentRequest, IpcVoid>(
            request,
            cancellationToken);
    }

    private Task PreloadSiteIconsAsync(VaultCipher[] ciphers)
    {
        var urls = ciphers
            .OfType<LoginVaultCipher>()
            .SelectMany(static c => c.Uris)
            .Select(static u => u.TryGetWebUri(out var uri) ? uri : null)
            .Where(static uri => uri is not null)
            .Cast<Uri>()
            .ToList();

        return iconCache.PreloadAsync(urls);
    }
}
using AsyncAwaitBestPractices;
using FluentBitwarden.Platform.Infrastructure.Connectivity;
using FluentBitwarden.Platform.Ipc.Abstractions;
using FluentBitwarden.Platform.Ipc.Transport;
using FluentBitwarden.Platform.SiteIcons;

namespace FluentBitwarden.Infrastructure.Clients;

[Fody.ConfigureAwait(false)]
internal sealed class RemoteVaultCipherClient(IIpcClient client, ISiteIconCache iconCache, INetworkStatus networkStatus) : IVaultCipherClient
{
    public async Task<VaultCipher[]> SearchCiphersAsync(VaultCipherQuery query, CancellationToken cancellationToken = default)
    {
        var result = await client.SendAsync<VaultCipherQuery, VaultCipher[]>(query, cancellationToken);
        if (networkStatus.HasInternetAccess)
            PreloadSiteIconsAsync(result).SafeFireAndForget();

        return result;
    }

    public Task<VaultCipher?> GetCipherAsync(GetVaultCipherRequest request, CancellationToken cancellationToken = default) =>
        client.SendAsync<GetVaultCipherRequest, VaultCipher?>(request, cancellationToken);

    public Task<VaultCipher> SaveCipherAsync(SaveVaultCipherRequest request, CancellationToken cancellationToken = default) =>
        client.SendAsync<SaveVaultCipherRequest, VaultCipher>(request, cancellationToken);

    public async Task DownloadCipherAttachmentAsync(
        DownloadVaultCipherAttachmentRequest request,
        CancellationToken cancellationToken = default)
    {
        _ = await client.SendAsync<DownloadVaultCipherAttachmentRequest, IpcVoid>(request, cancellationToken);
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

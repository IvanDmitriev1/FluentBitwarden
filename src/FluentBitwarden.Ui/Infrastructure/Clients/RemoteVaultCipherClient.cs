using AsyncAwaitBestPractices;
using FluentBitwarden.Platform.Infrastructure.Connectivity;
using FluentBitwarden.Platform.Ipc.Abstractions;
using FluentBitwarden.Platform.Ipc.Transport;
using FluentBitwarden.Platform.SiteIcons;

namespace FluentBitwarden.Infrastructure.Clients;

[Fody.ConfigureAwait(false)]
internal sealed class RemoteVaultCipherClient(IIpcClient client, ISiteIconCache iconCache, INetworkStatus networkStatus) : IVaultCipherClient
{
    public async Task<Cipher[]> SearchCiphersAsync(VaultCipherQuery query, CancellationToken cancellationToken = default)
    {
        var result = await client.SendAsync<VaultCipherQuery, Cipher[]>(query, cancellationToken);
        if (networkStatus.HasInternetAccess)
            PreloadSiteIconsAsync(result).SafeFireAndForget();

        return result;
    }

    public Task<Cipher?> GetCipherAsync(GetVaultCipherRequest request, CancellationToken cancellationToken = default) =>
        client.SendAsync<GetVaultCipherRequest, Cipher?>(request, cancellationToken);

    public Task<Cipher> SaveCipherAsync(SaveVaultCipherRequest request, CancellationToken cancellationToken = default) =>
        client.SendAsync<SaveVaultCipherRequest, Cipher>(request, cancellationToken);

    public async Task DownloadCipherAttachmentAsync(
        DownloadVaultCipherAttachmentRequest request,
        CancellationToken cancellationToken = default)
    {
        _ = await client.SendAsync<DownloadVaultCipherAttachmentRequest, IpcVoid>(request, cancellationToken);
    }

    private Task PreloadSiteIconsAsync(Cipher[] ciphers)
    {
        var urls = ciphers
            .OfType<LoginCipher>()
            .SelectMany(static c => c.Uris)
            .Select(static u => u.TryGetWebUri(out var uri) ? uri : null)
            .Where(static uri => uri is not null)
            .Cast<Uri>()
            .ToList();

        return iconCache.PreloadAsync(urls);
    }
}

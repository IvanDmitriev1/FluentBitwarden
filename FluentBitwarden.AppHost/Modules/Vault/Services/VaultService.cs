using BitwardenApi.Contracts;
using BitwardenApi.Models;
using FluentBitwarden.AppHost.Infrastructure.Abstractions;
using FluentBitwarden.Data.Abstractions;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.SshAgent.Models;
using FluentBitwarden.Modules.Vault.Abstractions;
using FluentBitwarden.Modules.Vault.Internal;
using FluentBitwarden.Modules.Vault.Internal.VaultDataParser;
using FluentBitwarden.Modules.Vault.Models;
using FluentBitwarden.Modules.Vault.Repositories;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace FluentBitwarden.Modules.Vault.Services;

[Fody.ConfigureAwait(false)]
internal sealed class VaultService(
    IUnitOfWorkFactory unitOfWorkFactory,
    IAccountSessionManager accountSessionManager,
    IConnectivityService connectivityService,
    ISiteIconCache siteIconCache,
    IVaultApiClient vaultApiClient) : IVaultService
{
    private readonly Dictionary<CipherId, VaultCipher> _ciphersById = new();
    private readonly List<VaultFolder> _folders = [];
    private readonly List<VaultCollection> _collections = [];

    private readonly Lock _lock = new();

    public event EventHandler<IVaultService, VaultChangedEventArgs>? VaultChanged;

    public void LoadLocalVault()
    {
        using var lockScope = _lock.EnterScope();

        using var unitOfWork = unitOfWorkFactory.Create();
        var decryptedUserKey = accountSessionManager.RequireActiveSession.DecryptedUserKey;

        _ciphersById.Clear();
        _folders.Clear();
        _collections.Clear();

        unitOfWork.VaultReaderRepository.ReadAllCiphers(
            decryptedUserKey.UserId,
            (_ciphersById, decryptedUserKey),
            static (state, ref readonly dto, payload) =>
            {
                var (ciphers, userKey) = state;

                var cipher = VaultDataParser.ParseAndDecryptCipher(in dto, payload, userKey);
                ciphers.Add(cipher.Id, cipher);
            });

        var folders = unitOfWork.VaultReaderRepository.GetAllFolders(decryptedUserKey.UserId)
            .Select(dto => VaultDataParser.ParseAndDecryptFolder(ref dto, decryptedUserKey));

        foreach (var folder in folders)
        {
            _folders.Add(folder);
        }

        if (connectivityService.HasInternetAccess)
        {
            var urls = _ciphersById.Values
                .OfType<LoginVaultCipher>()
                .Select(static c => c.Uris.FirstOrDefault())
                .Where(static s => !string.IsNullOrWhiteSpace(s))
                .Select(static s => Uri.TryCreate(s, UriKind.Absolute, out var uri) ? uri : null)
                .Where(static uri => uri is not null)
                .Cast<Uri>()
                .ToList();

            _ = PreloadSiteIconsAsync(urls);
        }


        VaultChanged?.Invoke(this, new VaultChangedEventArgs()
        {
            Kind = VaultChangedEventArgs.VaultChangeKind.FullReload,
            CipherId = CipherId.Empty
        });
    }

    private async Task PreloadSiteIconsAsync(IReadOnlyList<Uri> urls)
    {
        try
        {
            await siteIconCache.PreloadAsync(urls);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Site icon preload failed: {ex}");
        }
    }

    public async Task<VaultSyncResult> SyncVaultAsync(CancellationToken token)
    {
        var currentUserId = accountSessionManager.RequireActiveSession.Profile.UserId;

        try
        {
            if (!await HasRemoteChangesAsync(currentUserId, token))
                return VaultSyncResult.NoChanges;

            var response = await vaultApiClient.GetSyncAsync(token);

            using var unitOfWork = unitOfWorkFactory.Create();
            var repository = new VaultWriterRepository(unitOfWork.Transaction, currentUserId);
            repository.DeleteVaultData();

            repository.WriteFolders(CollectionsMarshal.AsSpan(response.Folders));
            repository.WriteCollections(CollectionsMarshal.AsSpan(response.Collections));
            repository.WriteCiphers(CollectionsMarshal.AsSpan(response.VaultCiphers));

            unitOfWork.AccountProfileRepository.UpdateSyncTime(currentUserId, DateTimeOffset.UtcNow);
            unitOfWork.SaveChanges();

            return VaultSyncResult.Synced;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Vault sync failed: {ex}");
            return VaultSyncResult.Failed;
        }
    }

    public VaultCipher? GetCipher(CipherId id)
    {
        using var _ = _lock.EnterScope();
        return _ciphersById.GetValueOrDefault(id);
    }

    public List<VaultCipher> GetCiphers(CipherQuery query)
    {
        using var _ = _lock.EnterScope();

        IEnumerable<VaultCipher> result = _ciphersById.Values;

        if (query.FavoritesOnly)
            result = result.Where(static x => x.Favorite);

        if (query.IncludeDeleted)
            result = result.Where(static x => x.DeletedDate is null);

        if (query.FolderId != FolderId.Empty)
            result = result.Where(x => x.FolderId == query.FolderId);

        if (query.CipherType is not null)
            result = result.Where(x => x.Type == query.CipherType.Value);

        if (!string.IsNullOrWhiteSpace(query.SearchText))
            result = result.Where(x => x.MatchesSearchText(query.SearchText));

        result = result.ApplySort(query.SortField, query.SortDirection);

        if (query.Limit is not null)
            result = result.Take(query.Limit.Value);

        return result.ToList();
    }

    public List<Fido2Credential> GetFido2Credentials(string rpId)
    {
        using var _ = _lock.EnterScope();

        return _ciphersById.Values
            .OfType<LoginVaultCipher>()
            .SelectMany(static cipher => cipher.Fido2Credentials)
            .Where(credential => credential.RpId == rpId)
            .ToList();
    }

    public List<SshPublicIdentityResponce> GetAvailableSshKeys()
    {
        using var _ = _lock.EnterScope();

        return _ciphersById.Values.OfType<SshKeyVaultCipher>()
            .Select(static c => new SshPublicIdentityResponce(c.PublicKey.KeyBlob, c.Name))
            .ToList();
    }

    public SshKeyVaultCipher? GetSsh(ReadOnlyMemory<byte> publicKeyBlob)
    {
        using var _ = _lock.EnterScope();

        return _ciphersById.Values.OfType<SshKeyVaultCipher>()
            .FirstOrDefault(c => c.PublicKey.KeyBlob.SequenceEqual(publicKeyBlob.Span));
    }

    public List<VaultFolder> GetFolders()
    {
        using var _ = _lock.EnterScope();

        return _folders
            .OrderBy(static x => x.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public List<VaultCollection> GetCollections()
    {
        using var _ = _lock.EnterScope();

        return _collections
            .OrderBy(static x => x.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private async Task<bool> HasRemoteChangesAsync(UserId currentUser, CancellationToken token)
    {
        using var unitOfWork = unitOfWorkFactory.Create();
        var lastSync = unitOfWork.AccountProfileRepository.GetLastSyncTime(currentUser);

        var revisionDate = await vaultApiClient.GetRevisionDateAsync(token);
        return lastSync < revisionDate;
    }
}

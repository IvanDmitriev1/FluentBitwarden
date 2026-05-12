using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Modules.Vault.Abstractions;
using BitwardenApi.Modules.Vault.Models;
using FluentBitwarden.Data.Abstractions;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Vault.Abstractions;
using FluentBitwarden.Modules.Vault.Internal;
using FluentBitwarden.Modules.Vault.Internal.SyncParser;
using FluentBitwarden.Modules.Vault.Internal.VaultDataParser;
using FluentBitwarden.Modules.Vault.Models;
using FluentBitwarden.Modules.Vault.Repositories;
using System.Diagnostics;
using System.Linq;
using FluentBitwarden.Modules.SshAgent.Models;

namespace FluentBitwarden.Modules.Vault.Services;

internal sealed class VaultService(
    IUnitOfWorkFactory unitOfWorkFactory,
    IAccountSessionManager accountSessionManager,
    IVaultApiClient vaultApiClient) : IVaultService
{
    private readonly Dictionary<CipherId, VaultCipher> _ciphersById = new();
    private readonly List<VaultFolder> _folders = [];
    private readonly List<VaultCollection> _collections = [];

    private readonly Lock _lock = new();

    public event EventHandler<IVaultService, VaultChangedEventArgs>? VaultChanged;

    public void LoadLocalVault()
    {
        using var _ = _lock.EnterScope();

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


        VaultChanged?.Invoke(this, new VaultChangedEventArgs()
        {
            Kind = VaultChangedEventArgs.VaultChangeKind.FullReload,
            CipherId = CipherId.Empty
        });
    }

    public async Task<VaultSyncResult> SyncVaultAsync(CancellationToken token)
    {
        var currentUserId = accountSessionManager.RequireActiveSession.Profile.UserId;

        try
        {
            if (!await HasRemoteChangesAsync(currentUserId, token))
                return VaultSyncResult.NoChanges;


            await vaultApiClient.GetSyncAsync(async stream =>
            {
                using var unitOfWork = unitOfWorkFactory.Create();

                var repository = new VaultWriterRepository(unitOfWork.Transaction, currentUserId);
                repository.DeleteVaultData();

                await VaultSyncResponseParser.ParseAsync(repository, stream, token);

                unitOfWork.AccountProfileRepository.UpdateSyncTime(currentUserId, DateTimeOffset.UtcNow);
                unitOfWork.SaveChanges();
            }, token);

            return VaultSyncResult.Synced;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Vault sync failed: {ex}");
            return VaultSyncResult.Failed;
        }
    }

    public IReadOnlyList<VaultCipher> GetCiphers()
    {
        using var _ = _lock.EnterScope();

        return _ciphersById.Values
            .Where(static x => x.DeletedDate is null)
            .OrderBy(static x => x.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public VaultCipher? GetCipher(CipherId id)
    {
        using var _ = _lock.EnterScope();
        return _ciphersById.GetValueOrDefault(id);
    }

    public IReadOnlyList<VaultCipher> Search(CipherQuery query)
    {
        using var _ = _lock.EnterScope();

        IEnumerable<VaultCipher> result = _ciphersById.Values;

        if (query.FavoritesOnly)
            result = result.Where(static x => x.Favorite);

        if (query.IncludeDeleted)
            result = result.Where(static x => x.DeletedDate is null);

        if (!query.IncludeArchived)
            result = result.Where(static x => x.DeletedDate is null);

        if (query.FolderId != FolderId.Empty)
            result = result.Where(x => x.FolderId == query.FolderId);

        if (query.CipherType is not null)
            result = result.Where(x => x.Type == query.CipherType.Value);

        if (!string.IsNullOrWhiteSpace(query.SearchText))
            result = result.Where(x => x.MatchesSearchText(query.SearchText));

        return result
            .OrderBy(static x => x.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<Fido2Credential> GetFido2Credentials(string rpId)
    {
        using var _ = _lock.EnterScope();

        return _ciphersById.Values
            .OfType<LoginVaultCipher>()
            .SelectMany(static cipher => cipher.Fido2Credentials)
            .Where(credential => credential.RpId == rpId)
            .ToList();
    }

    public IReadOnlyList<SshPublicIdentityResponce> GetAvailableSshKeys()
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

    public IReadOnlyList<VaultFolder> GetFolders()
    {
        using var _ = _lock.EnterScope();

        return _folders
            .OrderBy(static x => x.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<VaultCollection> GetCollections()
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
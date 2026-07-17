using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using BitwardenApi.Vault.Cryptography;
using Windows.Networking.Connectivity;
using FluentBitwarden.AppHost.Infrastructure.Extensions;
using FluentBitwarden.AppHost.Modules.Vault.Persistence.Parsing;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Internal;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Models;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;
using FluentBitwarden.AppHost.Modules.Sessions.Models;
using Microsoft.Extensions.Logging;
using FluentBitwarden.AppHost.Infrastructure.Data.Abstractions;

namespace FluentBitwarden.AppHost.Modules.Vault.Workspace;

[Fody.ConfigureAwait(false)]
internal sealed class VaultWorkspace(
    IUnitOfWorkFactory unitOfWorkFactory,
    IVaultItemsApi vaultApiClient,
    ILogger<VaultWorkspace> logger) : IVaultWorkspace
{
    public async Task<IUnlockedVault> LoadAsync(
        BitwardenAccountContext accountContext,
        UserKey userKey,
        KeySession keys,
        bool forceSync,
        CancellationToken cancellationToken)
    {
        var vault = Load(userKey, keys);
        if (!forceSync && !vault.IsEmpty)
            return vault;

        var syncResult = await SyncCoreAsync(accountContext, userKey, force: true, cancellationToken);

        return syncResult == VaultSyncResult.Synced ? Load(userKey, keys) : vault;
    }

    public async Task<VaultSyncOutcome> SyncAsync(
        BitwardenAccountContext accountContext,
        UserKey userKey,
        KeySession keys,
        IUnlockedVault current,
        CancellationToken cancellationToken)
    {
        var result = await SyncCoreAsync(accountContext, userKey, force: false, cancellationToken);

        return new VaultSyncOutcome(
            result,
            result == VaultSyncResult.Synced ? Load(userKey, keys) : current);
    }

    private UnlockedVault Load(UserKey decryptedUserKey, KeySession keys)
    {
        using var unitOfWork = unitOfWorkFactory.Create();
        var organizationKeys = unitOfWork.VaultReaderRepository.GetAllOrganizations(decryptedUserKey.UserId)
            .ToDictionary(static organization => organization.Id, static organization => organization.ProtectedOrganizationKey);

        var ciphersById = new Dictionary<CipherId, VaultCipher>();
        var cipherIdsByCollectionId = new Dictionary<CollectionId, HashSet<CipherId>>();

        unitOfWork.VaultReaderRepository.ReadAllCiphers(
            decryptedUserKey.UserId,
            (ref readonly dto, payload) =>
            {
                var key = keys.GetOrganizationKey(dto.OrganizationId, organizationKeys.GetValueOrDefault(dto.OrganizationId, AsymmetricEncString.Empty));
                var cipher = VaultDataParser.ParseAndDecryptCipher(in dto, payload, key);
                ciphersById.Add(cipher.Id, cipher);
                CipherCollectionIndex.Add(cipherIdsByCollectionId, dto.Id, dto.CollectionIds);
            });


        var folders = unitOfWork.VaultReaderRepository.GetAllFolders(decryptedUserKey.UserId)
            .Select(dto => VaultDataParser.ParseAndDecryptFolder(ref dto, decryptedUserKey))
            .ToList();

        var collectionDtos = unitOfWork.VaultReaderRepository.GetAllCollections(decryptedUserKey.UserId);
        var collections = new List<VaultCollection>(collectionDtos.Length);
        foreach (ref readonly var dto in collectionDtos.AsSpan())
        {
            var key = keys.GetOrganizationKey(dto.OrganizationId, organizationKeys.GetValueOrDefault(dto.OrganizationId, AsymmetricEncString.Empty));
            collections.Add(VaultDataParser.ParseAndDecryptCollection(in dto, key));
        }

        return new UnlockedVault(
            decryptedUserKey.UserId,
            new LoadedVaultData(ciphersById, cipherIdsByCollectionId, folders, collections));
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Intentional log-and-continue boundary; sync failures are reported as a VaultSyncResult, not thrown.")]
    private async Task<VaultSyncResult> SyncCoreAsync(
        BitwardenAccountContext accountContext,
        UserKey decryptedUserKey,
        bool force,
        CancellationToken cancellationToken)
    {
        if (!NetworkInformation.HasInternetAccess)
            return VaultSyncResult.SkippedOffline;

        try
        {
            if (!force && !await HasRemoteChangesAsync(accountContext, cancellationToken))
            {
                return VaultSyncResult.NoChanges;
            }

            var response = await vaultApiClient.GetSyncAsync(accountContext, cancellationToken);
            if (response.Profile.Id != decryptedUserKey.UserId)
                throw new InvalidDataException("Sync profile user id did not match the unlocked account.");

            using var unitOfWork = unitOfWorkFactory.Create();
            var userId = decryptedUserKey.UserId;
            var repository = unitOfWork.VaultWriterRepository;

            repository.WriteOrganizations(userId, response.Profile.Organizations);
            repository.WriteFolders(userId, response.Folders);
            repository.WriteCollections(userId, response.Collections);
            repository.WriteCiphers(userId, response.VaultCiphers);

            unitOfWork.AccountProfileRepository.UpdateSyncedProfile(decryptedUserKey.UserId, response.Profile);
            unitOfWork.SaveChanges();

            return VaultSyncResult.Synced;
        }
        catch (OperationCanceledException)
        {
            return VaultSyncResult.SkippedOffline;
        }
        catch (Exception e)
        {
            logger.SyncFailed(e);
            return VaultSyncResult.Failed;
        }
    }

    private async Task<bool> HasRemoteChangesAsync(
        BitwardenAccountContext accountContext,
        CancellationToken cancellationToken)
    {
        var revisionDate = await vaultApiClient.GetRevisionDateAsync(accountContext, cancellationToken);

        using var unitOfWork = unitOfWorkFactory.Create();
        var lastSync = unitOfWork.VaultReaderRepository.GetLastSyncTime(accountContext.UserId);
        if (lastSync is null)
            return true;

        var lastSyncTrunc = lastSync.Value.TruncateToSeconds();
        var revisionTrunc = revisionDate.TruncateToSeconds();

        return lastSyncTrunc < revisionTrunc;
    }

    /// <summary>
    /// Encrypts a decrypted domain <see cref="VaultCipher"/> and pushes it to the server, creating it
    /// when its id is empty and updating it otherwise. A fresh individual cipher key is generated on
    /// every save and wrapped with the user key. The server's response is persisted to the local
    /// cache, decrypted back into a domain <see cref="VaultCipher"/> and folded into the vault — no
    /// follow-up sync needed.
    /// </summary>
    public async Task<VaultCipherSaveOutcome> SaveCipherAsync(
        BitwardenAccountContext accountContext,
        UserKey userKey,
        IUnlockedVault current,
        VaultCipher cipher,
        CancellationToken cancellationToken)
    {
        var request = BuildRequest(userKey, cipher);

        var savedDto = cipher.Id.IsEmpty
            ? await vaultApiClient.CreateCipherAsync(accountContext, request, cancellationToken)
            : await vaultApiClient.UpdateCipherAsync(accountContext, cipher.Id, request, cancellationToken);

        using var unitOfWork = unitOfWorkFactory.Create();
        unitOfWork.VaultWriterRepository.UpsertCipher(userKey.UserId, ref savedDto);
        unitOfWork.SaveChanges();

        var savedCipher = VaultDataParser.ParseAndDecryptCipher(ref savedDto, savedDto.Data, userKey);

        // Total by construction: this class is the only place IUnlockedVault handles are made.
        return new VaultCipherSaveOutcome(savedCipher, ((UnlockedVault)current).With(savedCipher));

        static VaultCipherRequest BuildRequest(UserKey userKey, VaultCipher cipher)
        {
            const int cipherKeyByteLength = 64;
            Span<byte> cipherKey = stackalloc byte[cipherKeyByteLength];
            try
            {
                RandomNumberGenerator.Fill(cipherKey);

                var wrappedKey = EncString.Encrypt(cipherKey, userKey.Key);
                DateTime? lastKnownRevisionDate = cipher.Id.IsEmpty ? null : cipher.RevisionDate.UtcDateTime;

                return VaultCipherRequestFactory.Build(cipher, cipherKey, wrappedKey, userKey.UserId.Value, lastKnownRevisionDate);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(cipherKey);
            }
        }
    }
}

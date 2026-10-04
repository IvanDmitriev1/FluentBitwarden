using BitwardenApi.Infrastructure.Cryptography.Enc;
using BitwardenApi.Primitives;
using BitwardenApi.Vault.Items;
using BitwardenApi.Vault.Items.Contracts;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Internal;
using FluentBitwarden.AppHost.Modules.Vault.Persistence;
using FluentBitwarden.AppHost.Modules.Vault.Persistence.Parsing;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;
using FluentBitwarden.Platform.Infrastructure.Connectivity;

namespace FluentBitwarden.AppHost.Modules.Vault.Services;

internal sealed class VaultManager(
    VaultReaderRepository vaultReaderRepository,
    VaultWriterRepository vaultWriterRepository,
    VaultSyncStateRepository vaultSyncStateRepository,
    IVaultItemsApi vaultApiClient,
    IUnitOfWork unitOfWork,
    INetworkStatus networkStatus) : IVaultManager
{
    public IUnlockedVault Open(BitwardenAccountContext accountContext, IAccountKeySession keySession)
    {
        if (accountContext.UserId != keySession.UserId)
            throw new InvalidOperationException("The selected account and account key must have the same user ID.");

        var userId = keySession.UserId;
        var organizationKeys = vaultReaderRepository.LoadOrganizationKeys(userId);

        List<VaultCipher> ciphers = [];
        var parser = new VaultDataParser();

        vaultReaderRepository.ReadAllCiphers(keySession.UserId,
            (ref readonly dto, payload) =>
                ciphers.Add(DecryptCipher(in dto, payload, keySession, organizationKeys, parser)));

        var folders = vaultReaderRepository.GetAllFolders(userId)
            .Select(dto => VaultDataParser.ParseAndDecryptFolder(ref dto, keySession.UserKey))
            .ToList();

        var collectionDtos = vaultReaderRepository.GetAllCollections(userId);
        var collections = new List<VaultCollection>(collectionDtos.Length);
        foreach (ref readonly var dto in collectionDtos.AsSpan())
        {
            var key = keySession.GetOrganizationKey(dto.OrganizationId, organizationKeys.GetValueOrDefault(dto.OrganizationId, AsymmetricEncString.Empty));
            collections.Add(VaultDataParser.ParseAndDecryptCollection(in dto, key));
        }

        return new UnlockedVault(accountContext, new VaultData(ciphers, folders, collections));
    }

    public async Task<VaultSyncResult> SyncAsync(IUnlockedVault vault, CancellationToken ct)
    {
        if (!networkStatus.HasInternetAccess)
            return VaultSyncResult.SkippedOffline;

        try
        {
            var revisionDate = await vaultApiClient.GetRevisionDateAsync(vault.AccountContext, ct);
            var lastSync = vaultSyncStateRepository.GetServerRevisionDate(vault.AccountContext.UserId);
            if (lastSync is not null && lastSync.Value >= revisionDate)
                return VaultSyncResult.NoChanges;

            var response = await vaultApiClient.GetSyncAsync(vault.AccountContext, ct);
            if (response.Profile.Id != vault.AccountContext.UserId)
                throw new InvalidDataException("Sync profile user id did not match the unlocked account.");

            var userId = vault.AccountContext.UserId;

            unitOfWork.Begin();

            vaultWriterRepository.WriteOrganizations(userId, response.Profile.Organizations);
            vaultWriterRepository.WriteFolders(userId, response.Folders);
            vaultWriterRepository.WriteCollections(userId, response.Collections);
            vaultWriterRepository.WriteCiphers(userId, response.VaultCiphers);

            vaultSyncStateRepository.UpsertServerRevisionDate(userId, revisionDate);

            unitOfWork.Commit();

            return VaultSyncResult.Synced;
        }
        catch (OperationCanceledException) when(ct.IsCancellationRequested)
        {
            return VaultSyncResult.SkippedOffline;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            return VaultSyncResult.Failed;
        }
    }

    public async Task<VaultCipher> SaveCipherAsync(
        IUnlockedVault vault,
        IAccountKeySession keySession,
        VaultCipher cipher,
        CancellationToken ct)
    {
        var request = VaultCipherRequestFactory.BuildRequest(keySession.UserKey, cipher);

        Task<VaultCipherResponse> savedDtoTask = cipher.Id.IsEmpty
            ? vaultApiClient.CreateCipherAsync(vault.AccountContext, request, ct)
            : vaultApiClient.UpdateCipherAsync(vault.AccountContext, cipher.Id, request, ct);

        var savedDto = await savedDtoTask;

        var organizationKeys = vaultReaderRepository.LoadOrganizationKeys(vault.AccountContext.UserId);
        var savedCipher = DecryptCipher(
            in savedDto,
            savedDto.Data,
            keySession,
            organizationKeys, new VaultDataParser());

        unitOfWork.Begin();
        vaultWriterRepository.UpsertCipher(vault.AccountContext.UserId, ref savedDto);
        unitOfWork.Commit();
        return savedCipher;
    }

    private static VaultCipher DecryptCipher(
        ref readonly VaultCipherResponse dto,
        ReadOnlySpan<byte> payload,
        IAccountKeySession keySession,
        Dictionary<OrganizationId, AsymmetricEncString> organizationKeys,
        VaultDataParser parser)
    {
        var key = keySession.GetOrganizationKey(dto.OrganizationId,
            organizationKeys.GetValueOrDefault(dto.OrganizationId, AsymmetricEncString.Empty));
        return parser.ParseAndDecryptCipher(in dto, payload, key);
    }
}

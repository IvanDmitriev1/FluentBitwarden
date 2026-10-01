using BitwardenApi.Infrastructure.Cryptography.Enc;
using BitwardenApi.Vault.Items.Contracts;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Internal;
using FluentBitwarden.AppHost.Modules.Vault.Persistence;
using FluentBitwarden.AppHost.Modules.Vault.Persistence.Parsing;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;

namespace FluentBitwarden.AppHost.Modules.Vault.Services;

internal sealed class VaultManager(
    VaultReaderRepository vaultReaderRepository) : IVaultManager
{
    public IUnlockedVault Open(IAccountKeySession accountKeySession)
    {
        var organizationKeys = vaultReaderRepository.GetAllOrganizations(accountKeySession.UserId)
            .ToDictionary(static organization => organization.Id, static organization => organization.ProtectedOrganizationKey);

        List<VaultCipher> ciphers = [];
        var parser = new VaultDataParser();

        vaultReaderRepository.ReadAllCiphers(
            accountKeySession.UserId,
            (ref readonly dto, payload) =>
            {
                var key = accountKeySession.GetOrganizationKey(dto.OrganizationId,
                    organizationKeys.GetValueOrDefault(dto.OrganizationId, AsymmetricEncString.Empty));
                var cipher = parser.ParseAndDecryptCipher(in dto, payload, key);
                ciphers.Add(cipher);
            });

        var folders = vaultReaderRepository.GetAllFolders(accountKeySession.UserId)
            .Select(dto => VaultDataParser.ParseAndDecryptFolder(ref dto, accountKeySession.UserKey))
            .ToList();

        var collectionDtos = vaultReaderRepository.GetAllCollections(accountKeySession.UserId);
        var collections = new List<VaultCollection>(collectionDtos.Length);
        foreach (ref readonly var dto in collectionDtos.AsSpan())
        {
            var key = accountKeySession.GetOrganizationKey(dto.OrganizationId, organizationKeys.GetValueOrDefault(dto.OrganizationId, AsymmetricEncString.Empty));
            collections.Add(VaultDataParser.ParseAndDecryptCollection(in dto, key));
        }

        return new UnlockedVault(accountKeySession.UserId, new VaultData(ciphers, folders, collections));
    }

    public Task<VaultSyncResult> Sync(IUnlockedVault vault)
    {
        return Task.FromResult(VaultSyncResult.NoChanges);
    }

    public Task SaveCipher(IUnlockedVault vault, VaultCipher cipher)
    {
        return Task.CompletedTask;
    }
}

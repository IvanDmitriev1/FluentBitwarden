using BitwardenApi.Infrastructure.Cryptography.Enc;
using BitwardenApi.Vault.Cryptography;
using FluentBitwarden.AppHost.Data.Abstractions;
using FluentBitwarden.AppHost.Modules.Accounts.KeyManagement;
using FluentBitwarden.AppHost.Modules.Vault.Persistence.Parsing;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Models;

namespace FluentBitwarden.AppHost.Modules.Vault.Workspace.Internal;

internal sealed class VaultLoader(
    IUnitOfWorkFactory unitOfWorkFactory,
    IAccountKeyService accountKeyService)
{
    public LoadedVaultData Load(UserKey decryptedUserKey)
    {
        using var unitOfWork = unitOfWorkFactory.Create();
        var organizationKeys = unitOfWork.VaultReaderRepository.GetAllOrganizations(decryptedUserKey.UserId)
            .ToDictionary(static organization => organization.Id, static organization => organization.ProtectedOrganizationKey);

        var ciphersById = new Dictionary<CipherId, VaultCipher>();
        var cipherIdsByCollectionId = new Dictionary<CollectionId, HashSet<CipherId>>();

        unitOfWork.VaultReaderRepository.ReadAllCiphers(
            decryptedUserKey.UserId,
            (ciphersById, cipherIdsByCollectionId, accountKeyService, organizationKeys),
            static (state, ref readonly dto, payload) =>
            {
                var (ciphers, collectionIndex, keyService, orgKeys) = state;

                var key = keyService.GetOrganizationKey(dto.OrganizationId, orgKeys.GetValueOrDefault(dto.OrganizationId, AsymmetricEncString.Empty));
                var cipher = VaultDataParser.ParseAndDecryptCipher(in dto, payload, key);
                ciphers.Add(cipher.Id, cipher);
                AddCollectionMembership(collectionIndex, in dto);
            });


        var folders = unitOfWork.VaultReaderRepository.GetAllFolders(decryptedUserKey.UserId)
            .Select(dto => VaultDataParser.ParseAndDecryptFolder(ref dto, decryptedUserKey))
            .ToList();

        var collectionDtos = unitOfWork.VaultReaderRepository.GetAllCollections(decryptedUserKey.UserId);
        var collections = new List<VaultCollection>(collectionDtos.Length);
        for (int i = 0; i < collectionDtos.Length; i++)
        {
            ref readonly var dto = ref collectionDtos[i];
            var key = accountKeyService.GetOrganizationKey(dto.OrganizationId, organizationKeys.GetValueOrDefault(dto.OrganizationId, AsymmetricEncString.Empty));
            collections.Add(VaultDataParser.ParseAndDecryptCollection(in dto, key));
        }

        return new LoadedVaultData(ciphersById, cipherIdsByCollectionId, folders, collections);
    }

    private static void AddCollectionMembership(
        Dictionary<CollectionId, HashSet<CipherId>> collectionIndex,
        ref readonly VaultCipherResponse dto)
    {
        var collectionIds = dto.CollectionIds;
        if (collectionIds.Length == 0)
            return;

        foreach (var collectionId in collectionIds)
        {
            if (collectionId.IsEmpty)
                continue;

            if (!collectionIndex.TryGetValue(collectionId, out var cipherIds))
            {
                cipherIds = [];
                collectionIndex.Add(collectionId, cipherIds);
            }

            cipherIds.Add(dto.Id);
        }
    }
}

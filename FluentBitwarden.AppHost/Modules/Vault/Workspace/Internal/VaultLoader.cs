using FluentBitwarden.AppHost.Infrastructure.Data.Abstractions;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Models;
using FluentBitwarden.Modules.Vault.Internal.VaultDataParser;

namespace FluentBitwarden.AppHost.Modules.Vault.Workspace.Internal;

internal sealed class VaultLoader(IUnitOfWorkFactory unitOfWorkFactory)
{
    public LoadedVaultData Load(DecryptedUserKey decryptedUserKey)
    {
        using var unitOfWork = unitOfWorkFactory.Create();

        var organizationDtos = unitOfWork.VaultReaderRepository.GetAllOrganizations(decryptedUserKey.UserId);
        var accountKeyMaterial = unitOfWork.AccountKeyMaterialRepository.GetById(decryptedUserKey.UserId) ?? throw new ArgumentException("Account key material not found.");
        using var keyResolver = new VaultKeyResolver(decryptedUserKey, accountKeyMaterial, organizationDtos);

        var ciphersById = new Dictionary<CipherId, VaultCipher>();
        var cipherIdsByCollectionId = new Dictionary<CollectionId, HashSet<CipherId>>();

        unitOfWork.VaultReaderRepository.ReadAllCiphers(
            decryptedUserKey.UserId,
            (ciphersById, cipherIdsByCollectionId, keyResolver),
            static (state, ref readonly dto, payload) =>
            {
                var (ciphers, collectionIndex, resolver) = state;

                var key = resolver.GetKey(dto.OrganizationId);
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
            var key = keyResolver.GetKey(dto.OrganizationId);
            collections.Add(VaultDataParser.ParseAndDecryptCollection(in dto, key));
        }

        return new LoadedVaultData(ciphersById, cipherIdsByCollectionId, folders, collections);
    }

    private static void AddCollectionMembership(
        Dictionary<CollectionId, HashSet<CipherId>> collectionIndex,
        ref readonly VaultCipherDto dto)
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

using FluentBitwarden.AppHost.Infrastructure.Data.Abstractions;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Models;
using FluentBitwarden.Modules.Vault.Internal.VaultDataParser;

namespace FluentBitwarden.AppHost.Modules.Vault.Workspace.Internal;

internal sealed class VaultLoader(IUnitOfWorkFactory unitOfWorkFactory)
{
    public LoadedVaultData Load(DecryptedUserKey decryptedUserKey)
    {
        using var unitOfWork = unitOfWorkFactory.Create();

        var ciphersById = new Dictionary<CipherId, VaultCipher>();

        unitOfWork.VaultReaderRepository.ReadAllCiphers(
            decryptedUserKey.UserId,
            (ciphersById, decryptedUserKey),
            static (state, ref readonly dto, payload) =>
            {
                var (ciphers, userKey) = state;

                var cipher = VaultDataParser.ParseAndDecryptCipher(in dto, payload, userKey);
                ciphers.Add(cipher.Id, cipher);
            });


        var folders = unitOfWork.VaultReaderRepository.GetAllFolders(decryptedUserKey.UserId)
            .Select(dto => VaultDataParser.ParseAndDecryptFolder(ref dto, decryptedUserKey))
            .ToList();

        /*var collections = unitOfWork.VaultReaderRepository.GetAllCollections(decryptedUserKey.UserId)
            .Select(dto => VaultDataParser.ParseAndDecryptCollection(ref dto, decryptedUserKey))
            .ToList();*/

        return new LoadedVaultData(ciphersById, folders, []);
    }
}
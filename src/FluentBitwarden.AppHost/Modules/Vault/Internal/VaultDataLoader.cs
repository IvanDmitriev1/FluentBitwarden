using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Persistence;
using FluentBitwarden.AppHost.Modules.Vault.Persistence.Parsing;

namespace FluentBitwarden.AppHost.Modules.Vault.Internal;

internal static class VaultDataLoader
{
    public static VaultData LoadCached(
        VaultReaderRepository reader,
        UserId userId,
        IAccountKeySession keySession)
    {
        var parser = new VaultDataParser();
        List<Cipher> ciphers = [];

        reader.ReadAllCiphers(userId,
            (ref readonly dto, payload) => ciphers.Add(DecryptCipher(in dto, payload, keySession, parser)));

        var folders = reader.GetAllFolders(userId)
            .Select(dto => VaultDataParser.ParseAndDecryptFolder(ref dto, keySession.UserKey))
            .ToList();

        var collectionDtos = reader.GetAllCollections(userId);
        var collections = new List<Collection>(collectionDtos.Length);
        foreach (ref readonly var dto in collectionDtos.AsSpan())
        {
            var key = keySession.GetDecryptionKey(dto.OrganizationId);
            collections.Add(VaultDataParser.ParseAndDecryptCollection(in dto, key));
        }

        return new VaultData(ciphers, folders, collections);
    }

    public static VaultData DecryptResponse(VaultSyncResponse response, IAccountKeySession keySession)
    {
        var parser = new VaultDataParser();
        List<Cipher> ciphers = new(response.VaultCiphers.Length);

        foreach (ref readonly var dto in response.VaultCiphers.AsSpan())
        {
            ciphers.Add(DecryptCipher(in dto, dto.Data, keySession, parser));
        }

        var folders = response.Folders
            .Select(dto => VaultDataParser.ParseAndDecryptFolder(ref dto, keySession.UserKey))
            .ToList();

        var collections = new List<Collection>(response.Collections.Length);
        foreach (ref readonly var dto in response.Collections.AsSpan())
        {
            var key = keySession.GetDecryptionKey(dto.OrganizationId);
            collections.Add(VaultDataParser.ParseAndDecryptCollection(in dto, key));
        }

        return new VaultData(ciphers, folders, collections);
    }

    public static Cipher DecryptSavedCipher(
        ref readonly CipherResponse dto,
        IAccountKeySession keySession) =>
        DecryptCipher(in dto, dto.Data, keySession, new VaultDataParser());

    private static Cipher DecryptCipher(
        ref readonly CipherResponse dto,
        ReadOnlySpan<byte> payload,
        IAccountKeySession keySession,
        VaultDataParser parser)
    {
        var key = keySession.GetDecryptionKey(dto.OrganizationId);
        return parser.ParseAndDecryptCipher(in dto, payload, key);
    }
}

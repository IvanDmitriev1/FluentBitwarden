using BitwardenApi.Infrastructure.Cryptography.Enc;
using BitwardenApi.Vault.Cryptography;
using BitwardenApi.Vault.Items.Contracts;
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
        var organizationKeys = reader.LoadOrganizationKeys(userId);
        var parser = new VaultDataParser();
        List<VaultCipher> ciphers = [];
        reader.ReadAllCiphers(userId,
            (ref readonly dto, payload) => ciphers.Add(DecryptCipher(in dto, payload, keySession, organizationKeys, parser)));

        var folders = reader.GetAllFolders(userId)
            .Select(dto => VaultDataParser.ParseAndDecryptFolder(ref dto, keySession.UserKey))
            .ToList();

        var collectionDtos = reader.GetAllCollections(userId);
        var collections = new List<VaultCollection>(collectionDtos.Length);
        foreach (ref readonly var dto in collectionDtos.AsSpan())
        {
            var key = GetOrganizationKey(dto.OrganizationId, keySession, organizationKeys);
            collections.Add(VaultDataParser.ParseAndDecryptCollection(in dto, key));
        }

        return new VaultData(ciphers, folders, collections);
    }

    public static VaultData DecryptResponse(VaultSyncResponse response, IAccountKeySession keySession)
    {
        var organizationKeys = response.Profile.Organizations
            .ToDictionary(static organization => organization.Id, static organization => organization.ProtectedOrganizationKey);
        var parser = new VaultDataParser();
        List<VaultCipher> ciphers = new(response.VaultCiphers.Length);
        foreach (ref readonly var dto in response.VaultCiphers.AsSpan())
            ciphers.Add(DecryptCipher(in dto, dto.Data, keySession, organizationKeys, parser));

        var folders = response.Folders
            .Select(dto => VaultDataParser.ParseAndDecryptFolder(ref dto, keySession.UserKey))
            .ToList();

        var collections = new List<VaultCollection>(response.Collections.Length);
        foreach (ref readonly var dto in response.Collections.AsSpan())
        {
            var key = GetOrganizationKey(dto.OrganizationId, keySession, organizationKeys);
            collections.Add(VaultDataParser.ParseAndDecryptCollection(in dto, key));
        }

        return new VaultData(ciphers, folders, collections);
    }

    public static VaultCipher DecryptSavedCipher(
        ref readonly VaultCipherResponse dto,
        IAccountKeySession keySession,
        Dictionary<OrganizationId, AsymmetricEncString> organizationKeys) =>
        DecryptCipher(in dto, dto.Data, keySession, organizationKeys, new VaultDataParser());

    private static VaultCipher DecryptCipher(
        ref readonly VaultCipherResponse dto,
        ReadOnlySpan<byte> payload,
        IAccountKeySession keySession,
        Dictionary<OrganizationId, AsymmetricEncString> organizationKeys,
        VaultDataParser parser)
    {
        var key = GetOrganizationKey(dto.OrganizationId, keySession, organizationKeys);
        return parser.ParseAndDecryptCipher(in dto, payload, key);
    }

    private static SymmetricCryptoKey GetOrganizationKey(
        OrganizationId organizationId,
        IAccountKeySession keySession,
        Dictionary<OrganizationId, AsymmetricEncString> organizationKeys) =>
        keySession.GetOrganizationKey(
            organizationId,
            organizationKeys.GetValueOrDefault(organizationId, AsymmetricEncString.Empty));
}

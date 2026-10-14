using FluentBitwarden.AppHost.Modules.Vault.Persistence;

namespace FluentBitwarden.AppHost.IntegrationTests.Modules.Vault;

internal static class VaultRepositoryTestData
{
    internal static List<(VaultCipherResponse Response, byte[] Payload)> ReadCiphers(
        VaultReaderRepository reader,
        UserId userId)
    {
        var ciphers = new List<(VaultCipherResponse Response, byte[] Payload)>();
        reader.ReadAllCiphers(userId, (ref readonly dto, payload) => ciphers.Add((dto, payload.ToArray())));
        return ciphers;
    }
}

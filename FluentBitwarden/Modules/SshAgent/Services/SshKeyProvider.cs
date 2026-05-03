using System.Linq;
using BitwardenApi.Modules.Vault.Models;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.SshAgent.Abstractions;
using FluentBitwarden.Modules.SshAgent.Internal;
using FluentBitwarden.Modules.SshAgent.Models;
using FluentBitwarden.Modules.Vault.Abstractions;

namespace FluentBitwarden.Modules.SshAgent.Services;

internal sealed class SshKeyProvider(
    ICurrentSessionAccessor sessionAccessor,
    IVaultSyncService vaultSyncService) : ISshKeyProvider
{
    public IReadOnlyList<SshPublicIdentity> ListIdentities()
    {
        if (!sessionAccessor.IsAuthenticated)
            return [];

        return vaultSyncService.Ciphers.OfType<SshKeyCipher>().Select(static c =>
                !OpenSshPublicKey.TryParse(c.PublicKey, out var key)
                    ? throw new ArgumentException()
                    : new SshPublicIdentity(key, c.Name))
            .ToList();
    }

    public SshSignatureResult SignAsync(SshSignRequest request)
    {
        throw new NotImplementedException();
    }
}
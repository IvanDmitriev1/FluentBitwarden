using FluentBitwarden.AppHost.Modules.Sessions.Abstractions;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;
using FluentBitwarden.Contracts.Modules.Vault.Workspace;

namespace FluentBitwarden.AppHost.Modules.Vault.Workspace;

/// <summary>
/// Serves <see cref="IUnlockedVaultReader"/> off whichever session is currently installed.
/// </summary>
/// <remarks>
/// Reads take no gate. They resolve the session pointer once and then work against an immutable
/// vault handle, so a concurrent lock can never tear a result — it can only mean the answer came
/// from a vault that has since been dropped. Callers that need a read to be ordered against
/// unlock/lock must go through <see cref="IVaultSessionManager.WithSessionAsync"/> instead.
/// </remarks>
internal sealed class UnlockedVaultReader(IVaultSessionManager sessionManager) : IUnlockedVaultReader
{
    public VaultCipher? GetCipher(CipherId id) =>
        sessionManager.TryGetUnlockedSession(out var session) ? session.Vault.GetCipher(id) : null;

    public VaultCipher[] GetCiphers(VaultCipherQuery query) =>
        sessionManager.TryGetUnlockedSession(out var session) ? session.Vault.GetCiphers(query) : [];
}

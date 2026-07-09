using FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Internal;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Models;
using FluentBitwarden.Contracts.Modules.Vault.Workspace;
using System.Diagnostics.CodeAnalysis;

namespace FluentBitwarden.AppHost.Application.Sessions;

/// <summary>
/// Thread-safe holder of the single current <see cref="SessionSnapshot"/>. Owns the session
/// pointer and provides lock-free reads; every mutation replaces the whole snapshot so readers
/// always observe a coherent view. Also owns the <see cref="TransitionGate"/> that serializes
/// every mutation; the unlock-vs-lock generation race stays in <see cref="VaultSession"/>.
/// </summary>
internal sealed class SessionStore : IUnlockedVaultReader
{
    private SessionSnapshot? _session;

    /// <summary>Serializes session transitions (unlock/lock) and data mutations (sync/save).</summary>
    public SemaphoreSlim TransitionGate { get; } = new(1, 1);

    public bool TryGetSession([NotNullWhen(true)] out SessionSnapshot? session)
    {
        session = Volatile.Read(ref _session);
        return session is not null;
    }

    public SessionSnapshot GetSession() => !TryGetSession(out var session)
        ? throw new InvalidOperationException("No unlocked account is present")
        : session;

    /// <summary>Installs a new session, disposing whatever snapshot it replaces.</summary>
    public void Swap(SessionSnapshot session)
    {
        var previousSession = Interlocked.Exchange(ref _session, session);
        previousSession?.Dispose();
    }

    /// <summary>
    /// Clears and disposes the current session. Returns <see langword="true"/> if a session was
    /// present. The pointer is flipped first so readers fail fast instead of observing a snapshot
    /// that is being torn down.
    /// </summary>
    public bool Clear()
    {
        var previousSession = Interlocked.Exchange(ref _session, null);
        if (previousSession is null)
            return false;

        previousSession.Dispose();
        return true;
    }

    /// <summary>
    /// Replaces only the vault data of the current session. Never disposes the previous
    /// snapshot: it shares the user key with the new one. Callers must hold the gate.
    /// </summary>
    public void ReplaceData(SessionSnapshot session, LoadedVaultData data) =>
        Volatile.Write(ref _session, session with { Data = data });

    public VaultCipher? GetCipher(CipherId id) =>
        Volatile.Read(ref _session)?.Data.CiphersById.GetValueOrDefault(id);

    public VaultCipher[] GetCiphers(VaultCipherQuery query) =>
        Volatile.Read(ref _session)?.Data.FilterCiphers(query) ?? [];

    public VaultFolder[] GetFolders() =>
        Volatile.Read(ref _session)?.Data.Folders.ToArray() ?? [];
}

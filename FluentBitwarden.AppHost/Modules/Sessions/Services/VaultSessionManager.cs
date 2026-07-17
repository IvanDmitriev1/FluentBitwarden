using AsyncAwaitBestPractices;
using BitwardenApi.Vault.Cryptography;
using FluentBitwarden.AppHost.Modules.Accounts.Abstractions;
using FluentBitwarden.AppHost.Modules.Sessions.Abstractions;
using FluentBitwarden.AppHost.Modules.Sessions.Models;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;
using FluentBitwarden.Contracts.Modules.Accounts.Unlock;
using FluentBitwarden.Contracts.Modules.Vault;
using System.Diagnostics.CodeAnalysis;

namespace FluentBitwarden.AppHost.Modules.Sessions.Services;

[Fody.ConfigureAwait(false)]
internal sealed class VaultSessionManager(
    IAccountUnlockService accountUnlockService,
    IVaultWorkspace vaultWorkspace,
    IAccountStore accountStore,
    IIpcEventPublisher eventPublisher)
    : IVaultSessionManager
{
    private readonly AsyncLocal<bool> _transitionHeld = new();
    private readonly SemaphoreSlim _transitionGate = new(1, 1);
    private SessionSnapshot? _session;

    public event Action<VaultSessionStatus>? SessionStatusChanged;

    public bool TryGetUnlockedSession([NotNullWhen(true)] out SessionSnapshot? session)
    {
        session = Volatile.Read(ref _session);
        return session is not null;
    }

    public SessionSnapshot GetUnlockedSession() => !TryGetUnlockedSession(out var session)
        ? throw new InvalidOperationException("No unlocked account is present")
        : session;

    public Task<T> WithSessionAsync<T>(
        Func<SessionSnapshot, CancellationToken, Task<T>> work,
        T lockedResult,
        CancellationToken cancellationToken)
    {
        return RunExclusiveAsync(
            () => TryGetUnlockedSession(out var session)
                ? work(session, cancellationToken)
                : Task.FromResult(lockedResult),
            cancellationToken);
    }

    public Task<AccountUnlockOutcome> UnlockAsync(
        AccountUnlockRequest request,
        CancellationToken cancellationToken) =>
        RunExclusiveAsync(async () =>
        {
            if (TryGetUnlockedSession(out var current))
            {
                return current.Account.UserId == request.Account.UserId
                    ? new AccountUnlockOutcome.Success()
                    : new AccountUnlockOutcome.Failure("Lock the vault before switching accounts.");
            }

            var result = accountUnlockService.Unlock(request);
            if (!result.TryGetUserKey(out var userKey))
            {
                return result.Outcome;
            }

            var session = await CreateSessionAsync(request, userKey, cancellationToken);
            if (session is null)
            {
                return new AccountUnlockOutcome.Failure(
                    "Account profile details are not available. Connect to Bitwarden and unlock again.");
            }

            Volatile.Write(ref _session, session);
            return result.Outcome;
        }, cancellationToken);

    public Task LockAsync(CancellationToken cancellationToken = default) =>
        RunExclusiveAsync(() =>
        {
            SessionSnapshot? previous = _session;
            Volatile.Write(ref _session, null);
            previous?.Dispose();

        }, cancellationToken);

    private async Task<SessionSnapshot?> CreateSessionAsync(
        AccountUnlockRequest request,
        UserKey userKey,
        CancellationToken cancellationToken)
    {
        KeySession? keys = null;
        SessionSnapshot? session = null;
        try
        {
            var context = new BitwardenAccountContext(request.Account.UserId, request.Account.Environment);
            var keyMaterial = accountStore.GetKeyMaterial(context.UserId) ??
                              throw new InvalidOperationException(
                                  $"Account key material not found for user '{context.UserId}'.");

            keys = new KeySession(userKey, keyMaterial.ProtectedPrivateKey);

            bool forceSync = accountStore.GetAccountProfileDetails(context.UserId) is null;
            var vault = await vaultWorkspace.LoadAsync(context, userKey, keys, forceSync, cancellationToken);

            if (accountStore.GetAccountProfileDetails(context.UserId) is null)
            {
                return null;
            }

            session = new SessionSnapshot(request.Account, userKey, keys, vault);
            return session;
        }
        finally
        {
            if (session is null)
            {
                keys?.Dispose();
                userKey.Dispose();
            }
        }
    }

    private async Task<T> RunExclusiveAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        if (_transitionHeld.Value)
        {
            throw new InvalidOperationException(
                "The transition gate is not reentrant. Release it before running work that needs it again.");
        }

        await _transitionGate.WaitAsync(cancellationToken);
        _transitionHeld.Value = true;
        SessionSnapshot? sessionBefore = _session;
        try
        {
            return await action.Invoke();
        }
        finally
        {
            _transitionHeld.Value = false;
            _transitionGate.Release();
            NotifyIfSessionChanged(sessionBefore);
        }
    }

    private Task RunExclusiveAsync(Action action, CancellationToken cancellationToken) =>
        RunExclusiveAsync(() =>
        {
            action.Invoke();
            return Task.FromResult(true);
        }, cancellationToken);

    private void NotifyIfSessionChanged(SessionSnapshot? before)
    {
        var after = Volatile.Read(ref _session);
        if (ReferenceEquals(before, after))
        {
            return;
        }

        var status = after is null ? VaultSessionStatus.Locked : VaultSessionStatus.Unlocked;
        SessionStatusChanged?.Invoke(status);
        eventPublisher.PublishAsync(new VaultSessionStatusChangedEvent(status)).SafeFireAndForget();
    }
}

using System.Diagnostics.CodeAnalysis;
using FluentBitwarden.AppHost.Modules.Accounts.StoredAccounts;
using FluentBitwarden.AppHost.Modules.Accounts.Unlock.Abstractions;
using FluentBitwarden.AppHost.Modules.Accounts.Unlock.Methods;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Contracts.Modules.Accounts.Unlock;

namespace FluentBitwarden.AppHost.Modules.Accounts.Unlock;

internal sealed class AccountUnlockService(
    IStoredAccountStore storedAccountStore,
    MasterPasswordAccountUnlockMethod masterPasswordUnlockMethod,
    WindowsHelloAccountUnlockMethod windowsHelloUnlockMethod) : IAccountUnlockService, IUnlockedAccountAccessor, IBitwardenEnvironmentAccessor
{
    private TaskCompletionSource _whenUnlocked = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private DecryptedUserKey? _decryptedUserKey;

    [property: AllowNull]
    public AccountProfile CurrentAccount
    {
        get => field ?? throw new InvalidOperationException("No unlocked account is present");
        set;
    }

    public DecryptedUserKey UserKey => _decryptedUserKey ?? throw new InvalidOperationException();
    public bool HasUnlockedAccount => _decryptedUserKey is not null;
    public BitwardenEnvironment CurrentEnvironment => CurrentAccount.Environment;

    public AccountUnlockOutcome Unlock(AccountUnlockRequest request)
    {
        var keyMaterial = storedAccountStore.GetKeyMaterial(
            request.Account.UserId);

        if (keyMaterial is null)
            return new AccountUnlockOutcome.RequiresOnlineReauth();

        var result = request switch
        {
            AccountUnlockRequest.MasterPasswordRequest password =>
                masterPasswordUnlockMethod.Unlock(
                    keyMaterial,
                    password.MasterPassword),

            AccountUnlockRequest.WindowsHelloRequest windowsHello =>
                windowsHelloUnlockMethod.Unlock(
                    keyMaterial,
                    windowsHello.OwnerWindowHandle),

            _ => throw new ArgumentOutOfRangeException(nameof(request))
        };

        if (!result.TryGetPayload(out var decryptedUserKey))
            return result.Outcome;

        _decryptedUserKey = decryptedUserKey;
        CurrentAccount = request.Account;

        return new AccountUnlockOutcome.Success();
    }

    public void Lock()
    {
        _decryptedUserKey?.Dispose();
        CurrentAccount = null;
        _whenUnlocked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
using System.Diagnostics.CodeAnalysis;
using BitwardenApi.Vault.Cryptography;
using FluentBitwarden.Contracts.Modules.Accounts.Unlock;

namespace FluentBitwarden.AppHost.Modules.Accounts.Unlock;

internal readonly struct AccountUnlockResult
{
    private readonly DecryptedUserKey? _userKey;

    private AccountUnlockResult(
        AccountUnlockOutcome outcome,
        DecryptedUserKey? userKey)
    {
        Outcome = outcome;
        _userKey = userKey;
    }

    public static AccountUnlockResult WithoutUserKey(AccountUnlockOutcome outcome) => new(outcome, null);

    public static AccountUnlockResult WithUserKey(AccountUnlockOutcome outcome, DecryptedUserKey userKey) =>
        new(outcome, userKey);

    public AccountUnlockOutcome Outcome { get; }

    public bool TryGetUserKey([NotNullWhen(true)] out DecryptedUserKey? userKey)
    {
        userKey = _userKey;
        return userKey is not null;
    }
}

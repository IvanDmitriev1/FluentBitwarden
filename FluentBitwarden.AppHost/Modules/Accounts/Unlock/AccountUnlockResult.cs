using System.Diagnostics.CodeAnalysis;
using BitwardenApi.Vault.Cryptography;
using FluentBitwarden.Contracts.Modules.Accounts.Unlock;

namespace FluentBitwarden.AppHost.Modules.Accounts.Unlock;

internal readonly struct AccountUnlockResult
{
    private readonly UserKey? _userKey;

    private AccountUnlockResult(
        AccountUnlockOutcome outcome,
        UserKey? userKey)
    {
        Outcome = outcome;
        _userKey = userKey;
    }

    public static AccountUnlockResult WithoutUserKey(AccountUnlockOutcome outcome) => new(outcome, null);

    public static AccountUnlockResult WithUserKey(AccountUnlockOutcome outcome, UserKey userKey) =>
        new(outcome, userKey);

    public AccountUnlockOutcome Outcome { get; }

    public bool TryGetUserKey([NotNullWhen(true)] out UserKey? userKey)
    {
        userKey = _userKey;
        return userKey is not null;
    }
}

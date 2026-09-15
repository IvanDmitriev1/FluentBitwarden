using System.Diagnostics.CodeAnalysis;
using BitwardenApi.Vault.Cryptography;
using FluentBitwarden.Contracts.Modules.Accounts.Unlock;

namespace FluentBitwarden.AppHost.Modules.Accounts.Abstractions;

internal readonly struct AccountUnlockResult
{
    public static AccountUnlockResult WithoutUserKey(AccountUnlockOutcome outcome) => new(outcome, null);

    public static AccountUnlockResult WithUserKey(AccountUnlockOutcome outcome, UserKey userKey) => new(outcome, userKey);

    private AccountUnlockResult(
        AccountUnlockOutcome outcome,
        UserKey? userKey)
    {
        Outcome = outcome;
        _userKey = userKey;
    }

    private readonly UserKey? _userKey;

    public AccountUnlockOutcome Outcome { get; }

    public bool TryGetUserKey([NotNullWhen(true)] out UserKey? userKey)
    {
        userKey = _userKey;
        return userKey is not null;
    }
}

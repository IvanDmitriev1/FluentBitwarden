using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.ViewModels.Accounts.Unlock;

public sealed record UnlockPageParameter(
    IReadOnlyList<AccountProfile> Accounts,
    AccountProfile FavoriteAccountProfile);

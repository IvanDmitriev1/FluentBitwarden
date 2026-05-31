using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.Views.Unlock.Models;

public sealed record UnlockPageParameter(IReadOnlyList<AccountProfile> Accounts, AccountProfile FavoriteAccountProfile);

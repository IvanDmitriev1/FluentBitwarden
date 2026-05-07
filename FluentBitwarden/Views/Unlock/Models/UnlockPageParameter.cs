using FluentBitwarden.Modules.Account.Models;

namespace FluentBitwarden.Views.Unlock.Models;

public sealed record UnlockPageParameter(IReadOnlyList<AccountProfile> Accounts, AccountProfile FavoriteAccountProfile);
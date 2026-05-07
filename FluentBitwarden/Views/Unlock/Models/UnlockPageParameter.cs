using FluentBitwarden.Modules.Account.Models;
using FluentBitwarden.Modules.Security.Models.Unlock;

namespace FluentBitwarden.Views.Unlock.Models;

public sealed record UnlockPageParameter(
    IReadOnlyList<AccountProfile> Accounts,
    AccountProfile FavoriteAccountProfile,
    UnlockCapabilities FavoriteAccountUnlockCapabilities);
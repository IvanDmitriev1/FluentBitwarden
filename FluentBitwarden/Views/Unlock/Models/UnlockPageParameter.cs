using FluentBitwarden.Modules.Account.Models;
using FluentBitwarden.Modules.Security.Models.Unlock;

namespace FluentBitwarden.Views.Unlock.Models;

public sealed record UnlockPageParameter(
    IReadOnlyList<StoredAccount> Accounts,
    StoredAccount FavoriteAccount,
    UnlockCapabilities FavoriteAccountUnlockCapabilities);
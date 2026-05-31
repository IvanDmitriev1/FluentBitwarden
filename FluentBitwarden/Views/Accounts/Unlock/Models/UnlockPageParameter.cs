using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Views.Startup.Models;

namespace FluentBitwarden.Views.Accounts.Unlock.Models;

public sealed record UnlockPageParameter(
    IReadOnlyList<AccountProfile> Accounts,
    AccountProfile FavoriteAccountProfile,
    StartupFlowTarget StartupTarget = StartupFlowTarget.MainShell);

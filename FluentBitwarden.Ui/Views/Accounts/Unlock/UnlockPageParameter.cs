using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Views.Startup.Loading;

namespace FluentBitwarden.Views.Accounts.Unlock;

public sealed record UnlockPageParameter(
    IReadOnlyList<AccountProfile> Accounts,
    AccountProfile FavoriteAccountProfile,
    StartupFlowTarget StartupTarget = StartupFlowTarget.MainShell);

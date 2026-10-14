using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.Application.Models;

internal abstract record AppSessionResolution
{
    private AppSessionResolution() { }

    public sealed record LoggedOutResolution : AppSessionResolution;

    public sealed record LockedResolution(
        AccountProfile[] Accounts,
        AccountProfile SelectedAccount) : AppSessionResolution;

    public sealed record UnlockedResolution(
        AccountProfile Account) : AppSessionResolution;
}
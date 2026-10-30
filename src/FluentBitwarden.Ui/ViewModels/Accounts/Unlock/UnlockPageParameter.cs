using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.ViewModels.Accounts.Unlock;

public sealed record UnlockPageParameter(
    AccountProfile SelectedAccount);

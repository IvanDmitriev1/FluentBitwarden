using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.Ui.Screens.Shell;

public sealed class ShellViewModel(AccountProfile account)
{
    public AccountProfile Account => account;
}

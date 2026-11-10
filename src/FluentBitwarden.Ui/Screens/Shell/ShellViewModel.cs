using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.Ui.Screens.Shell;

public sealed partial class ShellViewModel(AccountProfile account) : ObservableObject
{
    public AccountProfile Account => account;
}

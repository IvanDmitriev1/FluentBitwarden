using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using Navis.WinUI.Abstractions;

namespace FluentBitwarden.Ui.Screens.Shell;

public sealed partial class ShellPageViewModel : ObservableObject, INavigationAware<AccountProfile>, INavigationAware
{
    [ObservableProperty]
    public partial AccountProfile? Account { get; private set; }

    public ValueTask OnNavigatedToAsync(AccountProfile account, CancellationToken cancellationToken)
    {
        Account = account;
        return ValueTask.CompletedTask;
    }

    public ValueTask OnNavigatedToAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public ValueTask OnNavigatedFromAsync(CancellationToken cancellationToken)
    {
        Account = null;
        return ValueTask.CompletedTask;
    }
}

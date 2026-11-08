using CommunityToolkit.Mvvm.ComponentModel;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.Ui.Screens.Unlock;

public sealed partial class UnlockViewModel(AccountProfile profile) : ObservableObject
{
    [ObservableProperty]
    public partial AccountProfile SelectedAccount { get; set; } = profile;
}

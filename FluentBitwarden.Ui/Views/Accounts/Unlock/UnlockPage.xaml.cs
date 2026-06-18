using FluentBitwarden.Services.Navigation;

namespace FluentBitwarden.Views.Accounts.Unlock;

public sealed partial class UnlockPage : LifecyclePage
{
    public UnlockPage(UnlockPageViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = ViewModel;
        InitializeComponent();
    }

    public UnlockPageViewModel ViewModel { get; }
}

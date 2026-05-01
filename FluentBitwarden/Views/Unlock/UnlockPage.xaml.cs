using FluentBitwarden.Shared.Behaviors.Lifecycle;

namespace FluentBitwarden.Views.Unlock;

public sealed partial class UnlockPage : LifecyclePage
{
    public UnlockPage(UnlockPageViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    public UnlockPageViewModel ViewModel { get; }
}

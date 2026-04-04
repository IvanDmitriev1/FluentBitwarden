using FluentBitwarden.Shared.Behaviors.Lifecycle;

namespace FluentBitwarden.Views.Setup;

public sealed partial class SetupPage : LifecyclePage
{
    public SetupPage(
        SetupPageViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    public SetupPageViewModel ViewModel { get; }
}

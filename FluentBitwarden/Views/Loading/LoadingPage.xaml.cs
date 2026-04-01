using FluentBitwarden.Shared.Behaviors.Lifecycle;

namespace FluentBitwarden.Views.Loading;

public sealed partial class LoadingPage : LifecyclePage
{
    public LoadingPage(LoadingPageViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    public LoadingPageViewModel ViewModel { get; }
}

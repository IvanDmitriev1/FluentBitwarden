using FluentBitwarden.Shared.Navigation.Lifecycle;

namespace FluentBitwarden.Views.Startup.Loading;

public sealed partial class LoadingPage : LifecyclePage
{
    public LoadingPage(LoadingPageViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = ViewModel;
        InitializeComponent();
    }

    public LoadingPageViewModel ViewModel { get; }
}

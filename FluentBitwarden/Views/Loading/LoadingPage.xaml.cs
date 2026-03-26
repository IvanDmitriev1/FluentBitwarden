using Microsoft.UI.Xaml.Controls;

namespace FluentBitwarden.Views.Loading;

public sealed partial class LoadingPage : Page
{
    public LoadingPage(LoadingPageViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;

        InitializeComponent();
    }

    public LoadingPageViewModel ViewModel { get; }
}
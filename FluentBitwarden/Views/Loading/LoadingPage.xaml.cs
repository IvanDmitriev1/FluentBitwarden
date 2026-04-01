using System.Security.Cryptography;
using Windows.Security.Credentials;
using FluentBitwarden.Shared.Behaviors.Lifecycle;
using Microsoft.UI.Xaml;

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

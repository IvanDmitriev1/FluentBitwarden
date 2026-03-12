using FluentBitwarden.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace FluentBitwarden.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsPage(SettingsPageViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    public SettingsPageViewModel ViewModel { get; }
}

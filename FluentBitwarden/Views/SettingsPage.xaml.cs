using FluentBitwarden.ViewModels;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics.CodeAnalysis;

namespace FluentBitwarden.Views;

[UnconditionalSuppressMessage(
    "Trimming",
    "IL2026",
    Justification = "Generated XAML bindings call validation-backed setters on trim-aware ObservableValidator viewmodels.")]
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

using FluentBitwarden.Ui.Controls;
using FluentBitwarden.ViewModels;

namespace FluentBitwarden.Views;

public sealed partial class SettingsPage : CorePage
{
    public SettingsPage(SettingsPageViewModel viewModel) : base(viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    public SettingsPageViewModel ViewModel { get; }
}

using FluentBitwarden.UI.Controls.Lifecycle;

namespace FluentBitwarden.Views.Settings;

public sealed partial class SettingsPage : LifecyclePage
{
    public SettingsPage(SettingsPageViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    public SettingsPageViewModel ViewModel { get; }
}
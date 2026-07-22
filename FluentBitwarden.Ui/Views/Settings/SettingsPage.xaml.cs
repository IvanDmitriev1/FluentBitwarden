
namespace FluentBitwarden.Views.Settings;

public sealed partial class SettingsPage : LifecyclePage
{
    public SettingsPage()
    {
        ViewModel = App.Current.GetRequiredService<SettingsPageViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
    }

    public SettingsPageViewModel ViewModel { get; }
}
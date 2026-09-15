
namespace FluentBitwarden.Views.Settings;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        ViewModel = App.Current.GetRequiredService<SettingsPageViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
    }

    public SettingsPageViewModel ViewModel { get; }
}

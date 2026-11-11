namespace FluentBitwarden.Ui.Screens.Setup.Steps.TwoFactor;

public sealed partial class SetupTwoFactorPage : Page
{
    public SetupTwoFactorPage()
    {
        ViewModel = App.Current.GetRequiredService<SetupTwoFactorPageViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
    }

    public SetupTwoFactorPageViewModel ViewModel { get; }
}

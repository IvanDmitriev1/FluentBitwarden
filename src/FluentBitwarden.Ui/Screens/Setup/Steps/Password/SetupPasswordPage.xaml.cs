namespace FluentBitwarden.Ui.Screens.Setup.Steps.Password;

public sealed partial class SetupPasswordPage : Page
{
    public SetupPasswordPage()
    {
        ViewModel = App.Current.GetRequiredService<SetupPasswordPageViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
    }

    public SetupPasswordPageViewModel ViewModel { get; }
}

namespace FluentBitwarden.Ui.Screens.Setup.Steps.Email;

public sealed partial class SetupEmailPage : Page
{
    public SetupEmailPage()
    {
        ViewModel = App.Current.GetRequiredService<SetupEmailPageViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
    }

    public SetupEmailPageViewModel ViewModel { get; }
}

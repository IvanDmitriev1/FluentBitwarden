namespace FluentBitwarden.Ui.Screens.Unlock;

public sealed partial class UnlockPage : Page
{
    public UnlockPage()
    {
        ViewModel = App.Current.GetRequiredService<UnlockPageViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
    }

    public UnlockPageViewModel ViewModel { get; }
}

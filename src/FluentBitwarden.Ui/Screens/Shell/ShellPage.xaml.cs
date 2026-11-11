namespace FluentBitwarden.Ui.Screens.Shell;

public sealed partial class ShellPage : Page
{
    public ShellPage()
    {
        ViewModel = App.Current.GetRequiredService<ShellPageViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
    }

    public ShellPageViewModel ViewModel { get; }
}

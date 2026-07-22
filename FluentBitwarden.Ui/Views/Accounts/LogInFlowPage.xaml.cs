namespace FluentBitwarden.Views.Accounts;

public sealed partial class LogInFlowPage : Page
{
    public LogInFlowPage()
    {
        ViewModel = App.Current.GetRequiredService<LogInFlowPageViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
    }

    public LogInFlowPageViewModel ViewModel { get; }

}
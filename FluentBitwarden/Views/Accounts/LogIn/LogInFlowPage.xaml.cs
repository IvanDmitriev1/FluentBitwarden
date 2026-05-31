namespace FluentBitwarden.Views.Accounts.LogIn;

public sealed partial class LogInFlowPage : Page
{
    public LogInFlowPage(LogInFlowPageViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = ViewModel;
        InitializeComponent();
    }

    public LogInFlowPageViewModel ViewModel { get; }

}
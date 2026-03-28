namespace FluentBitwarden.Views.Setup;

public sealed partial class SetupPage : Page
{

    public SetupPage(SetupPageViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    public SetupPageViewModel ViewModel { get; }
}
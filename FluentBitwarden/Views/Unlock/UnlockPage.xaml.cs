namespace FluentBitwarden.Views.Unlock;

public sealed partial class UnlockPage : Page
{
    public UnlockPage(UnlockPageViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }

    public UnlockPageViewModel ViewModel { get; }
}
namespace FluentBitwarden.Views.Startup;

public sealed partial class LoadingPage : LifecyclePage
{
    public LoadingPage(LoadingPageViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = ViewModel;
        InitializeComponent();
    }

    public LoadingPageViewModel ViewModel { get; }
}

using FluentBitwarden.ViewModels.Startup;
namespace FluentBitwarden.Views.Startup;

public sealed partial class LoadingPage : Page
{
    public LoadingPage()
    {
        ViewModel = App.Current.GetRequiredService<LoadingPageViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
    }

    public LoadingPageViewModel ViewModel { get; }
}

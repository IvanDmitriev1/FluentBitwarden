
namespace FluentBitwarden.Views.Accounts;

public sealed partial class UnlockPage : LifecyclePage
{
    public UnlockPage()
    {
        ViewModel = App.Current.GetRequiredService<UnlockPageViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
    }

    public UnlockPageViewModel ViewModel { get; }
}

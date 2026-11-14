using Microsoft.UI.Xaml.Input;

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

    private void FindKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = VaultCipherSearchBox.Focus(FocusState.Programmatic);
    }
}

using FluentBitwarden.Ui.Application;
using FluentBitwarden.Ui.Infrastructure.Windowing;
using Microsoft.UI.Xaml;

namespace FluentBitwarden.Ui.Windows;

public sealed partial class MainWindow : WinUIEx.WindowEx, IDialogProvidable
{
    public MainWindow()
    {
        ViewModel = App.Current.GetRequiredService<AppRootViewModel>();
        InitializeComponent();
    }

    public XamlRoot XamlRoot => RootElement.XamlRoot;

    public AppRootViewModel ViewModel { get; }
}

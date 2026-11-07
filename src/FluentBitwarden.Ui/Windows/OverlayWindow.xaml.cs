using FluentBitwarden.Ui.Application;
using FluentBitwarden.Ui.Infrastructure.Windowing;
using Microsoft.UI.Xaml;

namespace FluentBitwarden.Ui.Windows;

public sealed partial class OverlayWindow : WinUIEx.WindowEx, IDialogProvidable
{
    public OverlayWindow()
    {
        ViewModel = App.Current.GetRequiredService<AppRootViewModel>();
        InitializeComponent();
    }

    public AppRootViewModel ViewModel { get; }
    public XamlRoot XamlRoot => RootElement.XamlRoot;
}

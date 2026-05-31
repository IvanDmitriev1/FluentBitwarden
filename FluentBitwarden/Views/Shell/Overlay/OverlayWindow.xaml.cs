using FluentBitwarden.Infrastructure.Abstractions;
using FluentBitwarden.Infrastructure.Extensions;
using FluentBitwarden.Infrastructure.Implementations;
using FluentBitwarden.Views.Shell.Loading;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using WinUIEx;

namespace FluentBitwarden.Views.Shell.Overlay;

public sealed partial class OverlayWindow : WinUIEx.WindowEx
{
    private readonly IUiHostedServiceStarter _hostedServiceStarter;

    public OverlayWindow(
        IUiHostedServiceStarter hostedServiceStarter,
        NavigationService navigationService)
    {
        _hostedServiceStarter = hostedServiceStarter;
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(DragArea);

        navigationService.Initialize(Frame);
        this.PreventMaximizeOnTitleBarDoubleClick();
        this.CenterOnScreen();

        Frame.Navigate(typeof(LoadingPage));
    }

    public XamlRoot XamlRoot => RootElement.XamlRoot;

    public void ApplyTheme(ElementTheme themeMode)
    {
        RootElement.RequestedTheme = themeMode;
    }

    public void SetContent(Page content)
    {
        Frame.Content = content;
    }

    private void EscapeKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        Close();
    }
}

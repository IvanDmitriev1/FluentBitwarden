using FluentBitwarden.Shared.Navigation.Lifecycle;
using FluentBitwarden.Views.Startup.Loading;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using WinUIEx;

namespace FluentBitwarden.Views.Shell.Overlay;

public sealed partial class OverlayWindow : WinUIEx.WindowEx, IThemeChangeable
{
    public OverlayWindow(
        NavigationService navigationService)
    {
        InitializeComponent();

        navigationService.Initialize(Frame);
        this.PreventMaximizeOnTitleBarDoubleClick();
        this.CenterOnScreen();

        IsAlwaysOnTop = true;
        IsShownInSwitchers = false;
        IsResizable = false;
        IsMaximizable = false;
        IsMinimizable = false;
        IsResizable = false;

        IsTitleBarVisible = false;
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(DragArea);

        Frame.Navigate(
            typeof(LoadingPage),
            PageNavigationParameter.From(LoadingPageParameter.RequestHost));
    }

    public XamlRoot XamlRoot => RootElement.XamlRoot;
    public bool IsRequestHost { get; } = true;

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

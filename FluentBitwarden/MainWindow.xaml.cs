using Microsoft.UI.Windowing;
using FluentBitwarden.Ui.Abstractions;
using FluentBitwarden.Ui;
using WinRT.Interop;

namespace FluentBitwarden;

public sealed partial class MainWindow : WinUIEx.WindowEx
{
    public MainWindow(
        INavigationService navigationService,
        WindowHandleProvider windowHandleProvider)
    {
        InitializeComponent();
        navigationService.Initialize(ContentFrame);
        windowHandleProvider.SetWindowHandle(WindowNative.GetWindowHandle(this));

        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
    }
}

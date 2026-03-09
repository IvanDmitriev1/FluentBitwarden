using FluentBitwarden.Ui.Abstractions;
using FluentBitwarden.Ui.Services;
using Microsoft.UI.Windowing;
using WinRT.Interop;
using WinUIEx;

namespace FluentBitwarden;

public sealed partial class MainWindow : WinUIEx.WindowEx
{
    public MainWindow(
        INavigationService navigationService,
        INotificationService notificationService,
        WindowHandleProvider windowHandleProvider)
    {
        InitializeComponent();
        navigationService.Initialize(ContentFrame);
        notificationService.Initialize(NotificationHost);
        windowHandleProvider.SetWindowHandle(WindowNative.GetWindowHandle(this));

        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
    }
}

using Microsoft.UI.Windowing;
using FluentBitwarden.Views;
using FluentBitwarden.Ui.Abstractions;

namespace FluentBitwarden;

public sealed partial class MainWindow : WinUIEx.WindowEx
{
    public MainWindow(
        INavigationService navigationService)
    {
        InitializeComponent();
        navigationService.Initialize(ContentFrame);

        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
    }
}

using FluentBitwarden.Ui.Dialogs.Abstractions;
using FluentBitwarden.Ui.Windows.Abstractions;
using Microsoft.UI.Windowing;

namespace FluentBitwarden.Ui.Windows;

public sealed partial class MainWindow : WinUIEx.WindowEx, IThemeChangeable, IContentDialogHost
{
    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
    }

    private readonly SemaphoreSlim _dialogGate = new(1, 1);


    public Frame RootFrame => RootNavigationFrame;

    public void ApplyTheme(ElementTheme themeMode) => RootElement.RequestedTheme = themeMode;

    public async Task ShowDialogAsync(ContentDialog dialog, CancellationToken cancellationToken)
    {
        await _dialogGate.WaitAsync(cancellationToken);

        try
        {
            dialog.XamlRoot = RootElement.XamlRoot;
            await dialog.ShowAsync(ContentDialogPlacement.Popup).AsTask(cancellationToken);
        }
        finally
        {
            _dialogGate.Release();
        }
    }
}

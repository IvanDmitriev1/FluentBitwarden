using FluentBitwarden.Ui.Dialogs.Abstractions;
using FluentBitwarden.Ui.Windows.Abstractions;
using Microsoft.UI.Xaml.Input;
using Windows.Graphics;
using WinUIEx;

namespace FluentBitwarden.Ui.Windows;

public sealed partial class OverlayWindow : WinUIEx.WindowEx, IThemeChangeable, IContentDialogHost
{
    public OverlayWindow()
    {
        InitializeComponent();

        this.CenterOnScreen();

        IsAlwaysOnTop = true;
        IsShownInSwitchers = false;
        IsResizable = false;
        IsMaximizable = false;
        IsMinimizable = false;
        IsResizable = false;

        IsTitleBarVisible = false;
    }

    private readonly SemaphoreSlim _dialogGate = new(0, 1);
    private int _dialogWaiting;

    public Frame RootFrame => RootNavigationFrame;

    public void ApplyTheme(ElementTheme themeMode) => RootElement.RequestedTheme = themeMode;

    public async Task ShowDialogAsync(ContentDialog dialog, CancellationToken cancellationToken)
    {
        _dialogWaiting++;
        await _dialogGate.WaitAsync(cancellationToken);

        dialog.XamlRoot = RootElement.XamlRoot;
        RootElement.Children.Add(dialog);

        try
        {
            ResizeWindowToContent(dialog.Content as FrameworkElement);
            await dialog.ShowAsync(ContentDialogPlacement.InPlace).AsTask(cancellationToken);
        }
        finally
        {
            _dialogWaiting--;
            RootElement.Children.Remove(dialog);
            _dialogGate.Release();

            if (_dialogWaiting == 0)
            {
                Close();
            }
        }
    }

    private void EscapeKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        Close();
    }

    private void RootElement_Loaded(object sender, RoutedEventArgs e)
    {
        _dialogGate.Release();
    }

    private void ResizeWindowToContent(FrameworkElement? body)
    {
        if (body is null)
        {
            return;
        }

        const double dialogChromeHeight = 152; // Padding, buttons, and frame

        double scale = RootElement.XamlRoot.RasterizationScale;
        var client = AppWindow.ClientSize;

        // Measure at the same 400-DIP width.
        body.Measure(new Size(400, double.PositiveInfinity));

        double height = body.DesiredSize.Height + dialogChromeHeight;

        AppWindow.ResizeClient(new SizeInt32(
            client.Width, (int)Math.Ceiling(height * scale)));

        this.CenterOnScreen();
    }
}

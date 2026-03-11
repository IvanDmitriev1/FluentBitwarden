using FluentBitwarden.Abstractions;
using FluentBitwarden.Models.Vault;
using FluentBitwarden.Ui.Abstractions;
using FluentBitwarden.Views;
using FluentBitwarden.Views.Setup;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUIEx;

namespace FluentBitwarden;

public sealed partial class StartupSplashScreen : SplashScreen
{
    private readonly ILocalDeviceInfoProvider _deviceInfoProvider;

    internal StartupSplashScreen(
        MainWindow mainWindow,
        ILocalDeviceInfoProvider deviceInfoProvider)
        : base(mainWindow)
    {
        _deviceInfoProvider = deviceInfoProvider ?? throw new ArgumentNullException(nameof(deviceInfoProvider));

        InitializeComponent();

        Width = double.NaN;
        Height = double.NaN;
        IsAlwaysOnTop = true;
    }

    public string AppDisplayName => "FluentBitwarden";

    protected override async Task OnLoading()
    {
        try
        {
            await _deviceInfoProvider.InitializeAsync().ConfigureAwait(true);

            await Task.Delay(TimeSpan.FromSeconds(1));
        }
        catch (Exception ex)
        {
            await ShowFatalStartupErrorAsync(ex);
            Application.Current.Exit();
        }
    }

    private async Task ShowFatalStartupErrorAsync(Exception exception)
    {
        if (Root.XamlRoot is null)
        {
            return;
        }

        ContentDialog dialog = new()
        {
            XamlRoot = Root.XamlRoot,
            Title = "Startup failed",
            Content = $"FluentBitwarden couldn't finish startup.{Environment.NewLine}{Environment.NewLine}{exception.Message}",
            CloseButtonText = "Close app",
            DefaultButton = ContentDialogButton.Close,
        };

        await dialog.ShowAsync();
    }
}

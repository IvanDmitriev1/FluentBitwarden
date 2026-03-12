using FluentBitwarden.Abstractions;
using FluentBitwarden.Abstractions.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUIEx;

namespace FluentBitwarden;

public sealed partial class StartupSplashScreen : SplashScreen
{
    private readonly IServiceProvider _serviceProvider;

    internal StartupSplashScreen(
        MainWindow mainWindow,
        IServiceProvider serviceProvider)
        : base(mainWindow)
    {
        _serviceProvider = serviceProvider;

        InitializeComponent();
    }

    public string AppDisplayName => "FluentBitwarden";

    protected override async Task OnLoading()
    {
        try
        {
            var deviceInfoProvider = _serviceProvider.GetRequiredService<ILocalDeviceInfoProvider>();
            var dbInitializerService = _serviceProvider.GetRequiredService<IDbInitializerService>();

            await Task.WhenAll(
                deviceInfoProvider.InitializeAsync(),
                dbInitializerService.InitializeAsync()).ConfigureAwait(false);

            await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
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

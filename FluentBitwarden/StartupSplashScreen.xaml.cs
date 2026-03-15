using FluentBitwarden.Abstractions;
using FluentBitwarden.Abstractions.Storage;
using Microsoft.Extensions.DependencyInjection;
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

        Width = double.NaN;
        Height = double.NaN;
    }

    public string AppDisplayName => "FluentBitwarden";

    protected override async Task OnLoading()
    {
        var deviceInfoProvider = _serviceProvider.GetRequiredService<ILocalDeviceInfoProvider>();
        var dbInitializerService = _serviceProvider.GetRequiredService<IDbInitializerService>();

        await Task.WhenAll(
            deviceInfoProvider.InitializeAsync(),
            dbInitializerService.InitializeAsync()).ConfigureAwait(false);
    }
}

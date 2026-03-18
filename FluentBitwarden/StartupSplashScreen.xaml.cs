using FluentBitwarden.Abstractions;
using FluentBitwarden.Abstractions.Storage;
using FluentBitwarden.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using Windows.ApplicationModel;
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

        var q = PackageHelper.IsPackaged;

        string packageRoot = Package.Current.InstalledLocation.Path;
        string workerPath = Path.Combine(
            packageRoot,
            "FluentBitwarden.VaultWorker",
            "FluentBitwarden.VaultWorker.exe");

        Process.Start(new ProcessStartInfo
        {
            FileName = workerPath,
            Arguments = "--on-demand",
            UseShellExecute = false
        });

        await Task.WhenAll(
            deviceInfoProvider.InitializeAsync(),
            dbInitializerService.InitializeAsync()).ConfigureAwait(false);
    }
}

using AsyncAwaitBestPractices;
using FluentBitwarden.CommandPalette.Application;
using FluentBitwarden.Platform;
using FluentBitwarden.Platform.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FluentBitwarden.CommandPalette;

public static class Program
{
    private const string ComServerArgument = "-RegisterProcessAsComServer";

    [MTAThread]
    public static void Main(string[] args)
    {
        if (args.Length == 0 || !StringComparer.Ordinal.Equals(args[0], ComServerArgument))
            return;

        var builder = Host.CreateApplicationBuilder();
        builder.Services
            .AddAppLogging("commandpalette")
            .AddInfrastructureServices()
            .AddPlatformServices()
            .AddCommandPaletteApplicationServices();

        using var host = builder.Build();

        var logger = host.Services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("FluentBitwarden.CommandPalette");

        SafeFireAndForgetExtensions.SetDefaultExceptionHandling(logger.UnhandledException);

        host.Run();
    }
}

using FluentBitwarden.CommandPalette.Application;
using FluentBitwarden.Platform.SiteIcons;
using Microsoft.Extensions.Hosting;

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
            .AddInfrastructureServices()
            .AddSiteIconCache()
            .AddCommandPaletteApplicationServices();

        using var host = builder.Build();
        host.Run();
    }
}

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shmuelie.WinRTServer;
using Shmuelie.WinRTServer.CsWinRT;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace FluentBitwarden.CmdPal;

public static class Program
{
    private const string ComServerArgument = "-RegisterProcessAsComServer";

    [MTAThread]
    public static void Main(string[] args)
    {
        if (args.Length == 0 || !StringComparer.Ordinal.Equals(args[0], ComServerArgument))
            return;

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton<EmptyCommandsProvider>();
        builder.Services.AddSingleton(static services =>
            new FluentBitwardenCmdPalExtension(
                services.GetRequiredService<EmptyCommandsProvider>(),
                services.GetRequiredService<IHostApplicationLifetime>()));
        builder.Services.AddHostedService<CmdPalComServer>();

        using var host = builder.Build();
        host.Run();
    }
}

[Guid("284C1158-2735-42BB-A49F-269B620CB905")]
public sealed partial class FluentBitwardenCmdPalExtension(
    EmptyCommandsProvider provider,
    IHostApplicationLifetime applicationLifetime) : IExtension, IDisposable
{
    private int _disposed;

    public object? GetProvider(ProviderType providerType) =>
        providerType == ProviderType.Commands ? provider : null;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
            applicationLifetime.StopApplication();
    }
}

public sealed partial class EmptyCommandsProvider : CommandProvider
{
    public EmptyCommandsProvider() => DisplayName = "FluentBitwarden";

    public override ICommandItem[] TopLevelCommands() => [];
}

internal sealed partial class CmdPalComServer(
    FluentBitwardenCmdPalExtension extension) : IHostedService, IDisposable
{
    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed",
        Justification = "Disposed via ComServer.UnsafeDispose() in Dispose().")]
    private ComServer? _server;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _server = new ComServer();
        _server.RegisterClass<FluentBitwardenCmdPalExtension, IExtension>(() => extension);
        _server.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _server?.Stop();
        return Task.CompletedTask;
    }

    public void Dispose() => Interlocked.Exchange(ref _server, null)?.UnsafeDispose();
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FluentBitwarden.Platform.Tests.Ipc.Infrastructure;

internal static class IpcTestHostFactory
{
    public static async Task<IpcTestHost> StartAsync<THandler>(
        IpcAuthenticationLevel authenticationLevel = IpcAuthenticationLevel.SamePackage,
        Action<IServiceCollection>? configureServices = null,
        CancellationToken cancellationToken = default)
        where THandler : class, IIpcRequestsHandler
    {
        string pipeName = $"FluentBitwarden.Tests.{Guid.NewGuid():N}";
        var verifier = new TestIpcClientsVerifier(authenticationLevel);
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            DisableDefaults = true,
        });

        builder.ConfigureContainer(new DefaultServiceProviderFactory(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            }));

        builder.Services.AddLogging();
        builder.Services.AddSingleton<IIpcClientsVerifier>(verifier);
        configureServices?.Invoke(builder.Services);

        builder.Services.AddIpcServer(pipeName, handlers =>
            handlers.Add<THandler>());
        builder.Services.AddIpcClient(pipeName);

        IHost host = builder.Build();

        try
        {
            await host.StartAsync(cancellationToken);
            return new IpcTestHost(host, pipeName, verifier, cancellationToken);
        }
        catch
        {
            host.Dispose();
            throw;
        }
    }
}

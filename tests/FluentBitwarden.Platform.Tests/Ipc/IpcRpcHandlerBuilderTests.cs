using FluentBitwarden.Platform.Tests.Ipc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Platform.Tests.Ipc;

public class IpcRpcHandlerBuilderTests
{
    [Fact(Timeout = IpcTestHost.TimeoutMilliseconds)]
    public void Build_reuses_registered_endpoint_instances()
    {
        TestContext.Current.CancellationToken.ThrowIfCancellationRequested();
        var services = new ServiceCollection();
        services.AddSingleton<RpcShapesHandlerState>();
        var builder = new IpcRpcHandlerBuilder(services);

        builder.Add<RpcShapesHandler>();

        var firstBuild = builder.Build();
        var secondBuild = builder.Build();

        Assert.Equal(4, firstBuild.Count);
        Assert.Equal(firstBuild.Count, secondBuild.Count);

        foreach (ushort messageType in firstBuild.Keys)
            Assert.Same(firstBuild[messageType], secondBuild[messageType]);
    }

    [Fact(Timeout = IpcTestHost.TimeoutMilliseconds)]
    public void Add_registers_handler_as_scoped()
    {
        TestContext.Current.CancellationToken.ThrowIfCancellationRequested();
        var services = new ServiceCollection();
        var builder = new IpcRpcHandlerBuilder(services);

        builder.Add<RpcShapesHandler>();

        ServiceDescriptor handlerRegistration = Assert.Single(
            services,
            static descriptor => descriptor.ServiceType == typeof(RpcShapesHandler));
        Assert.Equal(ServiceLifetime.Scoped, handlerRegistration.Lifetime);
    }
}

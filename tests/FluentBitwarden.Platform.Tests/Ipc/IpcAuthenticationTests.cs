using FluentBitwarden.Platform.Tests.Ipc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Platform.Tests.Ipc;

public class IpcAuthenticationTests
{
    [Theory(Timeout = IpcTestHost.TimeoutMilliseconds)]
    [InlineData(IpcAuthenticationLevel.Rejected)]
    [InlineData(IpcAuthenticationLevel.PackagedExternalProxy)]
    public async Task Clients_outside_the_package_cannot_dispatch_requests(IpcAuthenticationLevel authenticationLevel)
    {
        CancellationToken testCancellation = TestContext.Current.CancellationToken;
        var state = new ImmediateEchoHandlerState();
        await using var host = await IpcTestHostFactory.StartAsync<ImmediateEchoHandler>(
            authenticationLevel,
            services => services.AddSingleton(state),
            testCancellation);

        await Assert.ThrowsAnyAsync<IOException>(async () =>
            await host.Client.SendAsync<EchoRequest, EchoResponse>(
                new EchoRequest(21, "rejected"),
                testCancellation));

        Assert.Equal(1, host.Verifier.InvocationCount);
        Assert.False(state.Completed.Task.IsCompleted);
    }

    [Fact(Timeout = IpcTestHost.TimeoutMilliseconds)]
    public async Task Same_package_authentication_reaches_the_registered_endpoint()
    {
        CancellationToken testCancellation = TestContext.Current.CancellationToken;
        var state = new ImmediateEchoHandlerState();
        await using var host = await IpcTestHostFactory.StartAsync<ImmediateEchoHandler>(
            IpcAuthenticationLevel.SamePackage,
            services => services.AddSingleton(state),
            testCancellation);

        EchoResponse response = await host.Client.SendAsync<EchoRequest, EchoResponse>(
            new EchoRequest(22, "accepted"),
            testCancellation);

        Assert.Equal(new EchoResponse(22, "accepted", 22), response);
        Assert.Equal(1, host.Verifier.InvocationCount);
        Assert.True(state.Completed.Task.IsCompletedSuccessfully);
    }
}

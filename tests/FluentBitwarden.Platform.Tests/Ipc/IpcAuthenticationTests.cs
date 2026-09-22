using FluentBitwarden.Platform.Tests.Ipc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Platform.Tests.Ipc;

public class IpcAuthenticationTests
{
    [Fact(Timeout = IpcTestHost.TimeoutMilliseconds)]
    public async Task Rejected_authentication_denies_dispatch_and_leaves_the_handler_untouched()
    {
        CancellationToken testCancellation = TestContext.Current.CancellationToken;
        var state = new ImmediateEchoHandlerState();
        await using var host = await IpcTestHostFactory.StartAsync<ImmediateEchoHandler>(
            IpcAuthenticationLevel.Rejected,
            services => services.AddSingleton(state),
            testCancellation);

        await Assert.ThrowsAnyAsync<IOException>(async () =>
            await host.Client.SendAsync<EchoRequest, EchoResponse>(
                new EchoRequest(21, "rejected"),
                testCancellation).AsTask());

        Assert.Equal(1, host.Verifier.InvocationCount);
        Assert.False(state.Completed.Task.IsCompleted);
    }

    [Fact(Timeout = IpcTestHost.TimeoutMilliseconds)]
    public async Task Accepted_authentication_reaches_the_registered_endpoint()
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

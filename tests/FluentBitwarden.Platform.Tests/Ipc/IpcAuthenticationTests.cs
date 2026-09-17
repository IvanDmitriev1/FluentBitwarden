using FluentBitwarden.Platform.Tests.Ipc.Infrastructure;

namespace FluentBitwarden.Platform.Tests.Ipc;

public class IpcAuthenticationTests
{
    [Fact(Timeout = 1000)]
    public async Task Rejected_authentication_denies_dispatch_and_leaves_the_handler_untouched()
    {
        await using var host = await IpcTestHost.StartAsync<ImmediateEchoHandler>(
            IpcAuthenticationLevel.Rejected);
        var handler = host.Handler<ImmediateEchoHandler>();
        CancellationToken testCancellation = TestContext.Current.CancellationToken;

        await Assert.ThrowsAnyAsync<IOException>(async () =>
            await host.Client.SendAsync<Infrastructure.EchoRequest, Infrastructure.EchoResponse>(
                new Infrastructure.EchoRequest(21, "rejected"),
                testCancellation).AsTask());

        Assert.Equal(1, host.Verifier.InvocationCount);
        Assert.False(handler.Completed.Task.IsCompleted);
    }

    [Fact(Timeout = 1000)]
    public async Task Accepted_authentication_reaches_the_registered_endpoint()
    {
        await using var host = await IpcTestHost.StartAsync<ImmediateEchoHandler>(
            IpcAuthenticationLevel.SamePackage);
        var handler = host.Handler<ImmediateEchoHandler>();
        CancellationToken testCancellation = TestContext.Current.CancellationToken;

        Infrastructure.EchoResponse response = await host.Client.SendAsync<Infrastructure.EchoRequest, Infrastructure.EchoResponse>(
            new Infrastructure.EchoRequest(22, "accepted"),
            testCancellation);

        Assert.Equal(new Infrastructure.EchoResponse(22, "accepted", 22), response);
        Assert.Equal(1, host.Verifier.InvocationCount);
        Assert.True(handler.Completed.Task.IsCompletedSuccessfully);
    }
}

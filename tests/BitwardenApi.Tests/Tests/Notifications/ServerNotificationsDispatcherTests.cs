using BitwardenApi.Notifications;
using BitwardenApi.Notifications.Abstractions;
using BitwardenApi.Notifications.Models;
using BitwardenApi.Tests.Infrastructure;
using Microsoft.AspNetCore.Connections;
using NSubstitute;

namespace BitwardenApi.Tests.Tests.Notifications;

public sealed class ServerNotificationsDispatcherTests
{
    [Fact]
    public async Task Factory_starts_account_connection_at_notifications_endpoint()
    {
        using var timeout = Timeout();
        await using var transport = new NotificationTestConnectionFactory();
        await using var services = new ServiceCollection().BuildServiceProvider();
        await using IServerNotificationsDispatcher dispatcher = CreateFactory(services).CreateDispatcher(TestApiSupport.AccountContext, transport.Configure);

        await transport.ConnectionCreated.WaitAsync(timeout.Token);
        Assert.Equal(1, transport.ConnectCount);
        Assert.Equal(Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets, transport.Transports);
        Assert.True(transport.SkipNegotiation);
        Assert.Equal(new Uri(TestApiSupport.AccountContext.Environment.NotificationsBase, "/hub"),
            Assert.IsAssignableFrom<UriEndPoint>(Assert.Single(transport.EndPoints)).Uri);
    }

    [Fact]
    public async Task Factory_instances_create_independent_connections()
    {
        using var timeout = Timeout();
        await using var firstTransport = new NotificationTestConnectionFactory();
        await using var secondTransport = new NotificationTestConnectionFactory();
        await using var services = new ServiceCollection().BuildServiceProvider();
        var factory = CreateFactory(services);
        await using IServerNotificationsDispatcher first = factory.CreateDispatcher(TestApiSupport.AccountContext, firstTransport.Configure);
        await using IServerNotificationsDispatcher second = factory.CreateDispatcher(TestApiSupport.AccountContext, secondTransport.Configure);

        await Task.WhenAll(firstTransport.ConnectionCreated, secondTransport.ConnectionCreated).WaitAsync(timeout.Token);
        Assert.Equal(1, firstTransport.ConnectCount);
        Assert.Equal(1, secondTransport.ConnectCount);
        Assert.NotSame(firstTransport.Connections[0], secondTransport.Connections[0]);
    }

    [Fact]
    public async Task Dispose_cancels_startup_while_transport_connect_is_pending()
    {
        using var timeout = Timeout();
        await using var transport = new NotificationTestConnectionFactory { BlockConnect = true };
        await using var services = new ServiceCollection().BuildServiceProvider();
        IServerNotificationsDispatcher dispatcher = CreateFactory(services).CreateDispatcher(TestApiSupport.AccountContext, transport.Configure);

        await transport.ConnectStarted.WaitAsync(timeout.Token);
        await dispatcher.DisposeAsync().AsTask().WaitAsync(timeout.Token);
        Assert.Equal(1, transport.ConnectCount);
    }

    [Fact]
    public async Task Factory_forwards_account_access_token()
    {
        using var timeout = Timeout();
        var tokenProvider = Substitute.For<IAccessTokenProvider>();
        tokenProvider.GetAccessTokenAsync(TestApiSupport.AccountContext).Returns(ValueTask.FromResult(SessionAccessToken.Parse("session-token")));
        await using var transport = new NotificationTestConnectionFactory { InvokeAccessTokenProvider = true };
        await using var services = new ServiceCollection().BuildServiceProvider();
        await using IServerNotificationsDispatcher dispatcher = CreateFactory(services, tokenProvider).CreateDispatcher(TestApiSupport.AccountContext, transport.Configure);

        await transport.ConnectionCreated.WaitAsync(timeout.Token);
        Assert.Equal("session-token", transport.AccessToken);
        await tokenProvider.Received(1).GetAccessTokenAsync(TestApiSupport.AccountContext);
    }

    [Fact]
    public async Task Async_start_failure_is_retried_and_disposal_cancels_retry()
    {
        using var timeout = Timeout();
        var time = new NotificationTestTimeProvider();
        await using var transport = new NotificationTestConnectionFactory(new HttpRequestException("offline"));
        await using var services = new ServiceCollection().BuildServiceProvider();
        IServerNotificationsDispatcher dispatcher = CreateFactory(services, timeProvider: time).CreateDispatcher(TestApiSupport.AccountContext, transport.Configure);

        Assert.Equal(TimeSpan.FromSeconds(30), await time.WaitForNextTimerAsync(timeout.Token));
        await dispatcher.DisposeAsync().AsTask().WaitAsync(timeout.Token);
        await time.AdvanceNextTimerAsync(timeout.Token);
        Assert.Equal(1, transport.ConnectCount);
        await dispatcher.DisposeAsync();
    }

    [Fact]
    public async Task Async_start_failure_retries_after_the_delay()
    {
        using var timeout = Timeout();
        var time = new NotificationTestTimeProvider();
        await using var transport = new NotificationTestConnectionFactory(new HttpRequestException("offline"));
        await using var services = new ServiceCollection().BuildServiceProvider();
        await using IServerNotificationsDispatcher dispatcher = CreateFactory(services, timeProvider: time).CreateDispatcher(TestApiSupport.AccountContext, transport.Configure);

        Assert.Equal(TimeSpan.FromSeconds(30), await time.WaitForNextTimerAsync(timeout.Token));
        await time.AdvanceNextTimerAsync(timeout.Token);
        await transport.ConnectionCreated.WaitAsync(timeout.Token);
        Assert.Equal(2, transport.ConnectCount);
    }

    [Fact]
    public async Task Dispose_closes_active_connection()
    {
        using var timeout = Timeout();
        await using var transport = new NotificationTestConnectionFactory();
        await using var services = new ServiceCollection().BuildServiceProvider();
        IServerNotificationsDispatcher dispatcher = CreateFactory(services).CreateDispatcher(TestApiSupport.AccountContext, transport.Configure);
        var connection = await transport.ConnectionCreated.WaitAsync(timeout.Token);

        await dispatcher.DisposeAsync();
        await connection.Disposed.WaitAsync(timeout.Token);
    }

    [Fact]
    public async Task Received_hub_message_is_deserialized_and_dispatched()
    {
        using var timeout = Timeout();
        var handled = new TaskCompletionSource<CipherChangedNotification>(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = Substitute.For<INotificationHandler<CipherChangedNotification>>();
        handler.HandleAsync(Arg.Any<NotificationType>(), Arg.Any<CipherChangedNotification>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                handled.TrySetResult(call.ArgAt<CipherChangedNotification>(1));
                return Task.CompletedTask;
            });
        await using var transport = new NotificationTestConnectionFactory();
        await using var services = new ServiceCollection().AddSingleton(handler).BuildServiceProvider();
        await using IServerNotificationsDispatcher dispatcher = CreateFactory(services).CreateDispatcher(TestApiSupport.AccountContext, transport.Configure);
        var connection = await transport.ConnectionCreated.WaitAsync(timeout.Token);

        await connection.SendHubMessageAsync("""
            {"type":1,"target":"ReceiveMessage","arguments":[{"contextId":"00000000-0000-0000-0000-000000000000","type":1,"payload":{"id":"cipher-1","userId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","collectionIds":[],"revisionDate":"2026-10-28T12:34:56Z"}}]}
            """, timeout.Token);

        CipherChangedNotification actual = await handled.Task.WaitAsync(timeout.Token);
        Assert.Equal(CipherId.Parse("cipher-1"), actual.Id);
        Assert.Equal(UserId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), actual.UserId);
        await handler.Received(1).HandleAsync(NotificationType.SyncCipherCreate, actual, CancellationToken.None);
    }

    private static ServerNotificationsDispatcherFactory CreateFactory(
        IServiceProvider services,
        IAccessTokenProvider? tokenProvider = null,
        TimeProvider? timeProvider = null)
    {
        if (tokenProvider is null)
        {
            tokenProvider = Substitute.For<IAccessTokenProvider>();
            tokenProvider.GetAccessTokenAsync(Arg.Any<BitwardenAccountContext>())
                .Returns(ValueTask.FromResult(SessionAccessToken.Parse("notifications-test-token")));
        }
        return new(tokenProvider, services.GetRequiredService<IServiceScopeFactory>(), timeProvider ?? TimeProvider.System);
    }

    private static CancellationTokenSource Timeout()
    {
        var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        return timeout;
    }
}

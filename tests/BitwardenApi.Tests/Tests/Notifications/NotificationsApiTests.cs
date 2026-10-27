using BitwardenApi.Notifications;
using BitwardenApi.Notifications.Internal;
using BitwardenApi.Notifications.Models;
using BitwardenApi.Tests.Infrastructure;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using NSubstitute;

namespace BitwardenApi.Tests.Tests.Notifications;

public sealed class NotificationsApiTests
{
    [Fact]
    public async Task Default_connection_configuration_honors_cancellation_before_network_access()
    {
        using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
        await using var api = new NotificationsApi(CreateAccessTokenProvider(), services);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            api.ConnectAsync(TestApiSupport.AccountContext, cancellation.Token));
    }

    [Fact]
    public async Task Connect_starts_the_hub_once_and_uses_the_notifications_endpoint()
    {
        using var timeout = CreateTimeoutTokenSource();
        using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
        await using var transport = new NotificationTestConnectionFactory();
        await using var api = new NotificationsApi(CreateAccessTokenProvider(), services, transport.Configure);

        await api.ConnectAsync(TestApiSupport.AccountContext, timeout.Token);
        await api.ConnectAsync(TestApiSupport.AccountContext, timeout.Token);

        Assert.Equal(1, transport.ConnectCount);
        UriEndPoint endPoint = Assert.IsAssignableFrom<UriEndPoint>(Assert.Single(transport.EndPoints));
        Assert.Equal(new Uri(TestApiSupport.AccountContext.Environment.NotificationsBase, "/hub"), endPoint.Uri);
    }

    [Fact]
    public async Task Connect_forwards_the_account_access_token_to_the_connection_options()
    {
        using var timeout = CreateTimeoutTokenSource();
        var accessTokenProvider = Substitute.For<IAccessTokenProvider>();
        accessTokenProvider.GetAccessTokenAsync(TestApiSupport.AccountContext, CancellationToken.None)
            .Returns(ValueTask.FromResult(SessionAccessToken.Parse("notifications-session-token")));
        using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
        await using var transport = new NotificationTestConnectionFactory
        {
            InvokeAccessTokenProvider = true,
        };
        await using var api = new NotificationsApi(accessTokenProvider, services, transport.Configure);

        await api.ConnectAsync(TestApiSupport.AccountContext, timeout.Token);

        Assert.Equal("notifications-session-token", transport.AccessToken);
        await accessTokenProvider.Received(1).GetAccessTokenAsync(
            TestApiSupport.AccountContext,
            CancellationToken.None);
    }

    [Fact]
    public async Task Connect_canceled_before_start_does_not_open_a_transport()
    {
        using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
        await using var transport = new NotificationTestConnectionFactory();
        await using var api = new NotificationsApi(CreateAccessTokenProvider(), services, transport.Configure);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => api.ConnectAsync(
            TestApiSupport.AccountContext,
            cancellation.Token));

        Assert.Equal(0, transport.ConnectCount);
    }

    [Fact]
    public async Task Disconnect_closes_the_active_connection_and_allows_a_fresh_connection()
    {
        using var timeout = CreateTimeoutTokenSource();
        using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
        await using var transport = new NotificationTestConnectionFactory();
        await using var api = new NotificationsApi(CreateAccessTokenProvider(), services, transport.Configure);

        await api.ConnectAsync(TestApiSupport.AccountContext, timeout.Token);
        NotificationTestConnectionFactory.NotificationTestConnection firstConnection = Assert.Single(transport.Connections);
        await api.DisconnectAsync();

        await firstConnection.Disposed.WaitAsync(TimeSpan.FromSeconds(5), timeout.Token);
        await api.ConnectAsync(TestApiSupport.AccountContext, timeout.Token);

        Assert.Equal(2, transport.ConnectCount);
        Assert.Equal(2, transport.Connections.Count);
    }

    [Fact]
    public async Task Dispose_closes_the_active_connection()
    {
        using var timeout = CreateTimeoutTokenSource();
        using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
        await using var transport = new NotificationTestConnectionFactory();
        await using var api = new NotificationsApi(CreateAccessTokenProvider(), services, transport.Configure);

        await api.ConnectAsync(TestApiSupport.AccountContext, timeout.Token);
        NotificationTestConnectionFactory.NotificationTestConnection connection = Assert.Single(transport.Connections);
        await api.DisposeAsync();

        await connection.Disposed.WaitAsync(TimeSpan.FromSeconds(5), timeout.Token);
    }

    [Fact]
    public async Task Connect_dispatches_a_received_hub_message_to_its_handler()
    {
        using var timeout = CreateTimeoutTokenSource();
        var delivered = new TaskCompletionSource<CipherChangedNotification>(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = Substitute.For<INotificationHandler<CipherChangedNotification>>();
        handler.HandleAsync(Arg.Any<CipherChangedNotification>()).Returns(call =>
        {
            delivered.TrySetResult(call.Arg<CipherChangedNotification>());
            return Task.CompletedTask;
        });
        using ServiceProvider services = new ServiceCollection().AddSingleton(handler).BuildServiceProvider();
        await using var transport = new NotificationTestConnectionFactory();
        await using var api = new NotificationsApi(CreateAccessTokenProvider(), services, transport.Configure);

        await api.ConnectAsync(TestApiSupport.AccountContext, timeout.Token);
        await Assert.Single(transport.Connections).SendHubMessageAsync("""
            {"type":1,"target":"ReceiveMessage","arguments":[{"contextId":"00000000-0000-0000-0000-000000000000","type":1,"payload":{"id":"cipher-1","userId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","isDeleted":true}}]}
            """, timeout.Token);

        CipherChangedNotification actual = await delivered.Task.WaitAsync(
            TimeSpan.FromSeconds(10),
            timeout.Token);

        Assert.Equal(new CipherChangedNotification(
            CipherId.Parse("cipher-1"),
            UserId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            true), actual);
        await handler.Received(1).HandleAsync(actual);
    }

    [Fact]
    public async Task Start_with_retry_retries_a_transient_connection_failure()
    {
        using var timeout = CreateTimeoutTokenSource();
        await using var transport = new NotificationTestConnectionFactory(new IOException("temporary connection failure"));
        await using HubConnection connection = CreateHubConnection(transport);
        var timeProvider = new NotificationTestTimeProvider();

        Task start = connection.StartWithRetryAsync(timeout.Token, timeProvider);

        Assert.Equal(TimeSpan.FromSeconds(2), await timeProvider.AdvanceNextTimerAsync(timeout.Token));
        await start.WaitAsync(timeout.Token);

        Assert.Equal(2, transport.ConnectCount);
        Assert.Equal([TimeSpan.FromSeconds(2)], timeProvider.ScheduledDelays);
    }

    [Fact]
    public async Task Start_with_retry_uses_the_default_clock_when_none_is_supplied()
    {
        using var timeout = CreateTimeoutTokenSource();
        await using var transport = new NotificationTestConnectionFactory(new IOException("temporary connection failure"));
        await using HubConnection connection = CreateHubConnection(transport);

        await connection.StartWithRetryAsync(timeout.Token);

        Assert.Equal(2, transport.ConnectCount);
        Assert.Equal(HubConnectionState.Connected, connection.State);
    }

    [Fact]
    public async Task Start_with_retry_cancels_while_waiting_to_retry()
    {
        using var timeout = CreateTimeoutTokenSource();
        await using var transport = new NotificationTestConnectionFactory(new IOException("temporary connection failure"));
        await using HubConnection connection = CreateHubConnection(transport);
        var timeProvider = new NotificationTestTimeProvider();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);

        Task start = connection.StartWithRetryAsync(cancellation.Token, timeProvider);

        Assert.Equal(TimeSpan.FromSeconds(2), await timeProvider.WaitForNextTimerAsync(timeout.Token));
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => start.WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken));
        Assert.Equal(1, transport.ConnectCount);
    }

    [Fact]
    public async Task Start_with_retry_caps_backoff_and_succeeds_after_repeated_failures()
    {
        using var timeout = CreateTimeoutTokenSource();
        TimeSpan[] expectedDelays =
        [
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(4),
            TimeSpan.FromSeconds(8),
            TimeSpan.FromSeconds(16),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(30),
        ];
        Exception[] failures = expectedDelays
            .Select(static (_, index) => (Exception)new IOException($"temporary failure {index + 1}"))
            .ToArray();
        await using var transport = new NotificationTestConnectionFactory(failures);
        await using HubConnection connection = CreateHubConnection(transport);
        var timeProvider = new NotificationTestTimeProvider();

        Task start = connection.StartWithRetryAsync(timeout.Token, timeProvider);

        foreach (TimeSpan expectedDelay in expectedDelays)
        {
            Assert.Equal(expectedDelay, await timeProvider.AdvanceNextTimerAsync(timeout.Token));
        }

        await start.WaitAsync(timeout.Token);

        Assert.Equal(expectedDelays.Length + 1, transport.ConnectCount);
        Assert.Equal(expectedDelays, timeProvider.ScheduledDelays);
    }

    private static IAccessTokenProvider CreateAccessTokenProvider()
    {
        var accessTokenProvider = Substitute.For<IAccessTokenProvider>();
        accessTokenProvider.GetAccessTokenAsync(
                Arg.Any<BitwardenAccountContext>(),
                Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(SessionAccessToken.Parse("notifications-test-token")));
        return accessTokenProvider;
    }

    private static CancellationTokenSource CreateTimeoutTokenSource()
    {
        var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        return timeout;
    }

    private static HubConnection CreateHubConnection(NotificationTestConnectionFactory transport)
    {
        IHubConnectionBuilder builder = new HubConnectionBuilder()
            .WithUrl(new Uri("http://notifications.bitwarden.test/hub"))
            .AddJsonProtocol()
            .WithAutomaticReconnect();
        transport.Configure(builder);
        return builder.Build();
    }
}

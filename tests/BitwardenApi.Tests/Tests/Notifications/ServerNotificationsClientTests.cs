using BitwardenApi.Notifications;
using BitwardenApi.Notifications.Abstractions;
using BitwardenApi.Notifications.Models;
using BitwardenApi.Tests.Infrastructure;
using NSubstitute;

namespace BitwardenApi.Tests.Tests.Notifications;

public sealed class ServerNotificationsClientTests
{
    [Fact]
    public async Task Connect_starts_the_hub_once_and_uses_the_notifications_endpoint()
    {
        using var timeout = CreateTimeoutTokenSource();
        await using var transport = new NotificationTestConnectionFactory();
        await using ServiceProvider services = CreateServices();
        await using var api = CreateClient(services, transport.Configure);

        await api.ConnectAsync(TestApiSupport.AccountContext);
        await api.ConnectAsync(TestApiSupport.AccountContext);

        Assert.Equal(1, transport.ConnectCount);
        Microsoft.AspNetCore.Connections.UriEndPoint endPoint = Assert.IsAssignableFrom<Microsoft.AspNetCore.Connections.UriEndPoint>(Assert.Single(transport.EndPoints));
        Assert.Equal(new Uri(TestApiSupport.AccountContext.Environment.NotificationsBase, "/hub"), endPoint.Uri);
    }

    [Fact]
    public async Task Connect_forwards_the_account_access_token_to_the_connection_options()
    {
        using var timeout = CreateTimeoutTokenSource();
        var accessTokenProvider = Substitute.For<IAccessTokenProvider>();
        accessTokenProvider.GetAccessTokenAsync(TestApiSupport.AccountContext)
            .Returns(ValueTask.FromResult(SessionAccessToken.Parse("notifications-session-token")));
        await using var transport = new NotificationTestConnectionFactory
        {
            InvokeAccessTokenProvider = true,
        };
        await using ServiceProvider services = CreateServices();
        await using var api = CreateClient(services, transport.Configure, accessTokenProvider);

        await api.ConnectAsync(TestApiSupport.AccountContext);

        Assert.Equal("notifications-session-token", transport.AccessToken);
        await accessTokenProvider.Received(1).GetAccessTokenAsync(TestApiSupport.AccountContext);
    }

    [Fact]
    public async Task Disconnect_closes_the_active_connection_and_allows_a_fresh_connection()
    {
        using var timeout = CreateTimeoutTokenSource();
        await using var transport = new NotificationTestConnectionFactory();
        await using ServiceProvider services = CreateServices();
        await using var api = CreateClient(services, transport.Configure);

        await api.ConnectAsync(TestApiSupport.AccountContext);
        NotificationTestConnectionFactory.NotificationTestConnection firstConnection = Assert.Single(transport.Connections);
        await api.DisconnectAsync();

        await firstConnection.Disposed.WaitAsync(TimeSpan.FromSeconds(5), timeout.Token);
        await api.ConnectAsync(TestApiSupport.AccountContext);

        Assert.Equal(2, transport.ConnectCount);
        Assert.Equal(2, transport.Connections.Count);
    }

    [Fact]
    public async Task Dispose_closes_the_active_connection()
    {
        using var timeout = CreateTimeoutTokenSource();
        await using var transport = new NotificationTestConnectionFactory();
        await using ServiceProvider services = CreateServices();
        await using var api = CreateClient(services, transport.Configure);

        await api.ConnectAsync(TestApiSupport.AccountContext);
        NotificationTestConnectionFactory.NotificationTestConnection connection = Assert.Single(transport.Connections);
        await api.DisposeAsync();

        await connection.Disposed.WaitAsync(TimeSpan.FromSeconds(5), timeout.Token);
    }

    [Fact]
    public async Task Connect_dispatches_a_received_hub_message_to_its_handler()
    {
        using var timeout = CreateTimeoutTokenSource();
        var delivered = new TaskCompletionSource<(NotificationType Type, CipherChangedNotification Notification)>(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = Substitute.For<INotificationHandler<CipherChangedNotification>>();
        handler.HandleAsync(Arg.Any<NotificationType>(), Arg.Any<CipherChangedNotification>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            delivered.TrySetResult((call.ArgAt<NotificationType>(0), call.ArgAt<CipherChangedNotification>(1)));
            return Task.CompletedTask;
        });
        await using var transport = new NotificationTestConnectionFactory();
        await using ServiceProvider services = CreateServices(handler);
        await using var api = CreateClient(services, transport.Configure);

        await api.ConnectAsync(TestApiSupport.AccountContext);
        await Assert.Single(transport.Connections).SendHubMessageAsync("""
            {"type":1,"target":"ReceiveMessage","arguments":[{"contextId":"00000000-0000-0000-0000-000000000000","type":1,"payload":{"id":"cipher-1","userId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","collectionIds":[],"revisionDate":"2026-10-28T12:34:56Z"}}]}
            """, timeout.Token);

        (NotificationType type, CipherChangedNotification actual) = await delivered.Task.WaitAsync(
            TimeSpan.FromSeconds(10),
            timeout.Token);

        Assert.Equal(CipherId.Parse("cipher-1"), actual.Id);
        Assert.Equal(UserId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), actual.UserId);
        Assert.Equal(OrganizationId.Empty, actual.OrganizationId);
        Assert.Empty(actual.CollectionIds);
        Assert.Equal(DateTimeOffset.Parse("2026-10-28T12:34:56Z"), actual.RevisionDate);
        Assert.Equal(NotificationType.SyncCipherCreate, type);
        await handler.Received(1).HandleAsync(NotificationType.SyncCipherCreate, actual, CancellationToken.None);
    }

    private static ServiceProvider CreateServices(INotificationHandler<CipherChangedNotification>? handler = null)
    {
        var services = new ServiceCollection();
        if (handler is not null)
            services.AddSingleton(handler);
        return services.BuildServiceProvider();
    }

    private static ServerNotificationsClient CreateClient(
        IServiceProvider services,
        Action<Microsoft.AspNetCore.SignalR.Client.IHubConnectionBuilder>? configure = null,
        IAccessTokenProvider? accessTokenProvider = null) =>
        new(accessTokenProvider ?? CreateAccessTokenProvider(), services.GetRequiredService<IServiceScopeFactory>(), TimeProvider.System, configure);

    private static IAccessTokenProvider CreateAccessTokenProvider()
    {
        var accessTokenProvider = Substitute.For<IAccessTokenProvider>();
        accessTokenProvider.GetAccessTokenAsync(Arg.Any<BitwardenAccountContext>())
            .Returns(ValueTask.FromResult(SessionAccessToken.Parse("notifications-test-token")));
        return accessTokenProvider;
    }

    private static CancellationTokenSource CreateTimeoutTokenSource()
    {
        var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        return timeout;
    }

}

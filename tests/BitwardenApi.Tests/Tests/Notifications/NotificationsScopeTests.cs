using BitwardenApi.Notifications;
using BitwardenApi.Notifications.Abstractions;
using BitwardenApi.Notifications.Internal;
using BitwardenApi.Notifications.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace BitwardenApi.Tests.Tests.Notifications;

public sealed class NotificationsScopeTests
{
    private const string CipherPayload = """{"id":"cipher-1","userId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","collectionIds":[],"revisionDate":"2026-10-28T12:34:56Z"}""";

    [Fact]
    public async Task Dispatch_supported_payload_without_handler_completes()
    {
        using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
        await using var client = CreateClient(services);

        await client.DispatchAsync(Envelope());
    }

    [Fact]
    public async Task Dispatch_uses_a_new_async_scope_per_delivery_and_disposes_each_handler()
    {
        var handlers = new List<ScopedHandler>();
        using ServiceProvider services = new ServiceCollection()
            .AddScoped<INotificationHandler<CipherChangedNotification>>(_ =>
            {
                var handler = new ScopedHandler();
                handlers.Add(handler);
                return handler;
            })
            .BuildServiceProvider();
        await using var client = CreateClient(services);

        await client.DispatchAsync(Envelope());
        await client.DispatchAsync(Envelope());

        Assert.Equal(2, handlers.Count);
        Assert.NotSame(handlers[0], handlers[1]);
        Assert.All(handlers, handler => Assert.True(handler.Disposed));
    }

    [Fact]
    public async Task Dispatch_propagates_handler_failure_and_disposes_scoped_handler()
    {
        var expected = new InvalidOperationException("handler failure");
        ScopedHandler? handler = null;
        using ServiceProvider services = new ServiceCollection()
            .AddScoped<INotificationHandler<CipherChangedNotification>>(_ => handler = new ScopedHandler(expected))
            .BuildServiceProvider();
        await using var client = CreateClient(services);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.DispatchAsync(Envelope()));

        Assert.Same(expected, actual);
        Assert.NotNull(handler);
        Assert.True(handler.Disposed);
    }

    private static ServerNotificationsDispatcher CreateClient(IServiceProvider services) =>
        new(new HubConnectionBuilder().WithUrl("http://localhost").Build(), services.GetRequiredService<IServiceScopeFactory>(), TimeProvider.System);

    private static NotificationEnvelope Envelope()
    {
        using JsonDocument document = JsonDocument.Parse(CipherPayload);
        return new NotificationEnvelope(default, NotificationType.SyncCipherCreate, document.RootElement.Clone());
    }

    private sealed class ScopedHandler(Exception? failure = null)
        : INotificationHandler<CipherChangedNotification>, IAsyncDisposable
    {
        public bool Disposed { get; private set; }

        public Task HandleAsync(
            NotificationType type,
            CipherChangedNotification notification,
            CancellationToken cancellationToken = default) =>
            failure is null ? Task.CompletedTask : Task.FromException(failure);

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

}

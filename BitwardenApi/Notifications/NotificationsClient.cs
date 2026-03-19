using System.Text.Json;
using BitwardenApi.Internal;
using Microsoft.AspNetCore.SignalR.Client;

namespace BitwardenApi.Notifications;

public sealed class NotificationsClient : INotificationsClient
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    private HubConnection? _connection;
    private Uri? _hubEndpoint;

    public event EventHandler<RawNotificationMessage>? OnEvent;

    public async Task ConnectAsync(
        ConnectNotificationsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.AccessToken.Value))
        {
            throw new ArgumentException("Access token cannot be empty.", nameof(request));
        }

        Uri requestedHubEndpoint = new(request.Context.Environment.NotificationsBase, "/hub");

        await _gate.WaitAsync(cancellationToken);

        try
        {
            bool requiresReconnect = _connection is not null
                && _hubEndpoint is not null
                && _hubEndpoint != requestedHubEndpoint;

            if (requiresReconnect)
            {
                await DisposeConnectionAsync(cancellationToken);
            }

            if (_connection is null)
            {
                _connection = CreateConnection(requestedHubEndpoint, request.AccessToken.Value);
                _hubEndpoint = requestedHubEndpoint;
            }

            if (_connection.State == HubConnectionState.Disconnected)
            {
                await _connection.StartAsync(cancellationToken);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            await DisposeConnectionAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _gate.Dispose();
    }

    private HubConnection CreateConnection(Uri hubEndpoint, string accessToken)
    {
        HubConnection connection = new HubConnectionBuilder()
            .WithUrl(hubEndpoint, options => options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken))
            .WithAutomaticReconnect()
            .Build();

        connection.On<JsonElement>("ReceiveMessage", payload => RaiseEvent("ReceiveMessage", payload.GetRawText()));
        connection.On<JsonElement>("ReceiveNotification", payload => RaiseEvent("ReceiveNotification", payload.GetRawText()));
        connection.On<string>("ReceiveMessage", payload => RaiseEvent(
            "ReceiveMessage",
            JsonSerializer.Serialize(payload, BitwardenApiJsonContext.Default.String)));
        connection.On<string>("ReceiveNotification", payload => RaiseEvent(
            "ReceiveNotification",
            JsonSerializer.Serialize(payload, BitwardenApiJsonContext.Default.String)));

        return connection;
    }

    private async Task DisposeConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is null)
        {
            _hubEndpoint = null;
            return;
        }

        try
        {
            if (_connection.State != HubConnectionState.Disconnected)
            {
                await _connection.StopAsync(cancellationToken);
            }
        }
        finally
        {
            await _connection.DisposeAsync();
            _connection = null;
            _hubEndpoint = null;
        }
    }

    private void RaiseEvent(string type, string rawJson)
    {
        RawNotificationMessage notificationEvent = new(type, rawJson, DateTimeOffset.UtcNow);
        OnEvent?.Invoke(this, notificationEvent);
    }
}

namespace BitwardenApi.Modules.Notifications.Models;

public sealed record ConnectNotificationsRequest(
    BitwardenClientContext Context,
    AccessToken AccessToken);

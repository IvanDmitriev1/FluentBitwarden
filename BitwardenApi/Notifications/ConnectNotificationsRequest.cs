using BitwardenApi.Primitives;

namespace BitwardenApi.Notifications;

public sealed record ConnectNotificationsRequest(
    BitwardenClientContext Context,
    AccessToken AccessToken);

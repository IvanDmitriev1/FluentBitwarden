using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Shared.Context;

namespace BitwardenApi.Modules.Notifications.Models;

public sealed record ConnectNotificationsRequest(
    BitwardenClientContext Context,
    AccessToken AccessToken);

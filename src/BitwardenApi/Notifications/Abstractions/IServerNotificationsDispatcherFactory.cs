using BitwardenApi.Client;
using Microsoft.AspNetCore.SignalR.Client;

namespace BitwardenApi.Notifications.Abstractions;

public interface IServerNotificationsDispatcherFactory
{
    IServerNotificationsDispatcher CreateDispatcher(BitwardenAccountContext accountContext, Action<IHubConnectionBuilder>? configureConnection = null);
}

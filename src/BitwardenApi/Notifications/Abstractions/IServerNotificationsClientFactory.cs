namespace BitwardenApi.Notifications.Abstractions;

public interface IServerNotificationsClientFactory
{
    IServerNotificationsClient CreateClient();
}

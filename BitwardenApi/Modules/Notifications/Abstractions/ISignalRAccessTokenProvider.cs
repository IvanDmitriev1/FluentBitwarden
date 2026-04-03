namespace BitwardenApi.Modules.Notifications.Abstractions;

public interface ISignalRAccessTokenProvider
{
    Task<string?> GetAccessToken();
}
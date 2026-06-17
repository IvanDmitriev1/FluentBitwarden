namespace BitwardenApi.Notifications.Contracts;

public interface ISignalRAccessTokenProvider
{
    Task<string?> GetAccessToken();
}
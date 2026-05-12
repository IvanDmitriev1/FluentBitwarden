namespace BitwardenApi.Contracts;

public interface ISignalRAccessTokenProvider
{
    Task<string?> GetAccessToken();
}
namespace BitwardenApi.Client;

public interface IAccessTokenProvider
{
    ValueTask<SessionAccessToken> GetAccessTokenAsync(
        BitwardenAccountContext accountContext,
        CancellationToken cancellationToken = default);
}

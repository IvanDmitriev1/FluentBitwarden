namespace BitwardenApi.Infrastructure.Transport;

public interface IBitwardenAccessTokenProvider
{
    ValueTask<SessionAccessToken> GetAccessTokenAsync(
        BitwardenAccountContext accountContext,
        CancellationToken cancellationToken = default);
}

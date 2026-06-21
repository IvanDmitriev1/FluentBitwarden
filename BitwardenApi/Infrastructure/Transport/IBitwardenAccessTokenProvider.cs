namespace BitwardenApi.Infrastructure.Transport;

public interface IBitwardenAccessTokenProvider
{
    ValueTask<AccessToken> GetAccessTokenAsync(
        BitwardenAccountContext accountContext,
        CancellationToken cancellationToken = default);
}

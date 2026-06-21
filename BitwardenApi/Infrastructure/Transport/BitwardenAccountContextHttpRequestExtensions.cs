namespace BitwardenApi.Infrastructure.Transport;

internal static class BitwardenAccountContextHttpRequestExtensions
{
    private static readonly HttpRequestOptionsKey<BitwardenAccountContext> AccountContextKey =
        new("BitwardenApi.AccountContext");

    public static void SetBitwardenAccountContext(
        this HttpRequestMessage request,
        BitwardenAccountContext accountContext) =>
        request.Options.Set(AccountContextKey, accountContext);

    public static BitwardenAccountContext GetBitwardenAccountContext(
        this HttpRequestMessage request) =>
        request.Options.TryGetValue(AccountContextKey, out var accountContext)
            ? accountContext
            : throw new InvalidOperationException(
                "The authenticated Bitwarden request does not contain an account context.");
}

namespace FluentBitwarden.Extensions;

internal static class ExceptionExtensions
{
    public static string ToOfflineVaultMessage(this Exception exception)
        => string.IsNullOrWhiteSpace(exception.Message)
            ? "The vault could not reach Bitwarden. Cached data is still available offline."
            : exception.Message;
}

namespace FluentBitwarden.ViewModels;

internal static class AuthErrorMessageFormatter
{
    public static string Format(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            InvalidOperationException => exception.Message,
            _ when !string.IsNullOrWhiteSpace(exception.Message) => exception.Message,
            _ => "Something went wrong. Try again.",
        };
    }
}

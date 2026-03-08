namespace BitwaredApi.Abstractions.Exceptions;

public sealed class NetworkUnavailableException : Exception
{
    public NetworkUnavailableException(string? message = null, Exception? innerException = null)
        : base(message ?? "The Bitwarden service could not be reached.", innerException)
    {
    }
}

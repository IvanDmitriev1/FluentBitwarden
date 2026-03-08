namespace BitwaredApi.Abstractions.Exceptions;

public sealed class ServerVersionMismatchException : Exception
{
    public ServerVersionMismatchException(string? message = null, Exception? innerException = null)
        : base(message ?? "The server returned a response that this client does not understand.", innerException)
    {
    }
}

namespace BitwaredApi.Abstractions.Exceptions;

public sealed class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException(string? message = null, Exception? innerException = null)
        : base(message ?? "The supplied credentials were rejected by the server.", innerException)
    {
    }
}

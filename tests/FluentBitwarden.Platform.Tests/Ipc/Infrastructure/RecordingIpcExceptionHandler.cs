namespace FluentBitwarden.Platform.Tests.Ipc.Infrastructure;

internal sealed class RecordingIpcExceptionHandler(Exception? exceptionToThrow = null) : IIpcExceptionHandler
{
    public List<Exception> Exceptions { get; } = [];

    public void Handle(Exception exception)
    {
        Exceptions.Add(exception);

        if (exceptionToThrow is not null)
            throw exceptionToThrow;
    }
}

namespace FluentBitwarden.Platform.Ipc.Abstractions;

public interface IIpcExceptionHandler
{
    void Handle(Exception exception);
}

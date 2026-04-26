namespace FluentBitwarden.Shared.Ipc.Abstractions;

public interface IIpcPipeServer
{
    Task RunAsync();
}
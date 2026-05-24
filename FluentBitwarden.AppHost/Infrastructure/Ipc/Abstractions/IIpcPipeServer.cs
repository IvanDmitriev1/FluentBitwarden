namespace FluentBitwarden.Infrastructure.Ipc.Abstractions;

public interface IIpcPipeServer
{
    Task RunAsync();
}
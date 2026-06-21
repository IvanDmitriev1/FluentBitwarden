using System.IO.Pipes;

namespace FluentBitwarden.Platform.Ipc.Abstractions;

internal interface IIpcClientsVerifier
{
    IpcAuthenticationLevel IsExpectedClient(NamedPipeServerStream pipe);
}
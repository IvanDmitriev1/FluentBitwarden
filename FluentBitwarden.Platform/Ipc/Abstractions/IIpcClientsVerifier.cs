using System.IO.Pipes;

namespace FluentBitwarden.Platform.Ipc.Abstractions;

internal interface IIpcClientsVerifier
{
    bool IsExpectedClient(NamedPipeServerStream pipe, out IpcAuthenticationLevel authenticationLevel);
}
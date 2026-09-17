using System.IO.Pipes;

namespace FluentBitwarden.Platform.Tests.Ipc.Infrastructure;

internal sealed class TestIpcClientsVerifier(IpcAuthenticationLevel authenticationLevel) : IIpcClientsVerifier
{
    private int _invocationCount;

    public IpcAuthenticationLevel AuthenticationLevel { get; set; } = authenticationLevel;

    public int InvocationCount => Volatile.Read(ref _invocationCount);

    public IpcAuthenticationLevel IsExpectedClient(NamedPipeServerStream pipe)
    {
        Interlocked.Increment(ref _invocationCount);
        return AuthenticationLevel;
    }
}

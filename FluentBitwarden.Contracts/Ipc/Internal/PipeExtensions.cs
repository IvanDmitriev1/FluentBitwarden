using System.ComponentModel;
using System.IO.Pipes;
using System.Runtime.InteropServices;

namespace FluentBitwarden.Contracts.Ipc.Internal;

internal static class PipeExtensions
{
    public static uint GetClientProcessId(this NamedPipeServerStream pipe)
    {
        return !PInvoke.GetNamedPipeClientProcessId(
            pipe.SafePipeHandle,
            out var clientProcessId)
            ? throw new Win32Exception(Marshal.GetLastPInvokeError())
            : clientProcessId;
    }
}
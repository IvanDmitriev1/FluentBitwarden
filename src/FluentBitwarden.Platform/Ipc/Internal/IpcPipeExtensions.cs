using System.ComponentModel;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using Windows.Win32.System.Threading;

namespace FluentBitwarden.Platform.Ipc.Internal;

internal static class IpcPipeExtensions
{
    public static uint GetClientProcessId(this NamedPipeServerStream pipe)
    {
        return !PInvoke.GetNamedPipeClientProcessId(
            pipe.SafePipeHandle,
            out var clientProcessId)
            ? throw new Win32Exception(Marshal.GetLastPInvokeError())
            : clientProcessId;
    }

    public static SafeFileHandle OpenClientProcess(uint processId)
    {
        HANDLE handle = PInvoke.OpenProcess(
            PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION,
            false,
            processId);

        return handle.IsNull
            ? throw new Win32Exception(Marshal.GetLastPInvokeError())
            : new SafeFileHandle(handle, ownsHandle: true);
    }

    public static string? TryGetPackageFamilyName(this SafeFileHandle processHandle)
    {
        uint length = 0;

        var result = PInvoke.GetPackageFamilyName(
            processHandle,
            ref length,
            Span<char>.Empty);

        if (result != WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER)
            return null;

        Span<char> packageFamilyNameBuffer = stackalloc char[(int)length];
        result = PInvoke.GetPackageFamilyName(
            processHandle,
            ref length,
            packageFamilyNameBuffer);

        if (result != WIN32_ERROR.ERROR_SUCCESS)
            return null;

        return packageFamilyNameBuffer.TrimEnd('\0').ToString();
    }

    public static string? TryGetProcessImagePath(this SafeFileHandle processHandle)
    {
        Span<char> buffer = stackalloc char[256];
        uint bufferLength = (uint)buffer.Length;

        var result = PInvoke.QueryFullProcessImageName(
            processHandle,
            0,
            buffer,
            ref bufferLength);

        if (!result)
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        }

        var processImagePath = buffer[..(int)bufferLength].ToString();
        return processImagePath;
    }
}
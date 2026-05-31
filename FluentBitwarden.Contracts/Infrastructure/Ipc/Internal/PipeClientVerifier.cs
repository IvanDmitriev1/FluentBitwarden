using System.ComponentModel;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using Windows.ApplicationModel;
using Windows.Win32.System.Threading;
using FluentBitwarden.Contracts.Infrastructure.Shared;

namespace FluentBitwarden.Contracts.Infrastructure.Ipc.Internal;

internal static class PipeClientVerifier
{
    private static readonly string[] ExpectedExeNames =
    [
        "FluentBitwarden.ComServer.exe",
        "FluentBitwarden.Ui.exe"
    ];

    public static bool IsExpectedClient(NamedPipeServerStream pipe)
    {
        if (!pipe.IsConnected)
            return false;

        var processId = pipe.GetClientProcessId();
        using var processHandle = OpenClientProcess(processId);
        if (processHandle.IsInvalid)
        {
            Debug.WriteLine($"IPC client rejected. Could not open process {processId}.");
            return false;
        }

        var clientPackageFamilyName = TryGetPackageFamilyName(processHandle);
        var expectedPackageFamilyName = Package.Current.Id.FamilyName;

        Debug.WriteLine($"Expected PFN: {expectedPackageFamilyName}");
        Debug.WriteLine($"Client PFN: {clientPackageFamilyName ?? "<none>"}");

        if (!StringComparer.OrdinalIgnoreCase.Equals(clientPackageFamilyName, expectedPackageFamilyName))
        {
            return false;
        }

        var clientExePath = TryGetProcessImagePath(processHandle);
        var clientFileName = Path.GetFileName(clientExePath);
        var clientBaseDirectory = Directory.GetParent(Path.GetDirectoryName(clientExePath)!)!;
        var clientBaseDirectoryPath = Path.GetFullPath(clientBaseDirectory.FullName);

        return StringComparer.OrdinalIgnoreCase.Equals(clientBaseDirectoryPath, PackageHelper.AppBasePath) &&
               ExpectedExeNames.Contains(clientFileName);
    }

    private static SafeFileHandle OpenClientProcess(uint processId)
    {
        HANDLE handle = PInvoke.OpenProcess(
            PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION,
            false,
            processId);

        return handle.IsNull
            ? throw new Win32Exception(Marshal.GetLastPInvokeError())
            : new SafeFileHandle(handle, ownsHandle: true);
    }

    private static string? TryGetPackageFamilyName(SafeFileHandle processHandle)
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

    private static string? TryGetProcessImagePath(SafeFileHandle processHandle)
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
            int error = Marshal.GetLastPInvokeError();
            Debug.WriteLine($"QueryFullProcessImageName failed: {error}");
            return null;
        }

        var processImagePath = buffer[..(int)bufferLength].ToString();
        return processImagePath;
    }
}

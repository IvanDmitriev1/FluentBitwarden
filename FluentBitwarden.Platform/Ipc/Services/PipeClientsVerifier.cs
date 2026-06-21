using FluentBitwarden.Platform.Infrastructure.Extensions;
using System.IO.Pipes;
using Windows.ApplicationModel;

namespace FluentBitwarden.Platform.Ipc.Services;

internal sealed class PipeClientsVerifier : IIpcClientsVerifier
{
    private static readonly string[] ExpectedPackagedExeNames =
    [
        "FluentBitwarden.ComServer.exe",
        "FluentBitwarden.Ui.exe",
        "FluentBitwarden.AppHost.exe",
    ];

    public bool IsExpectedClient(NamedPipeServerStream pipe, out IpcAuthenticationLevel authenticationLevel)
    {
        authenticationLevel = IpcAuthenticationLevel.Anonymous;

        if (!pipe.IsConnected)
            return false;

        var processId = pipe.GetClientProcessId();
        using var processHandle = IpcPipeExtensions.OpenClientProcess(processId);
        if (processHandle.IsInvalid)
        {
            Debug.WriteLine($"IPC client rejected. Could not open process {processId}.");
            return false;
        }

        var clientPackageFamilyName = processHandle.TryGetPackageFamilyName();
        var expectedPackageFamilyName = Package.Current.Id.FamilyName;

        if (!StringComparer.OrdinalIgnoreCase.Equals(clientPackageFamilyName, expectedPackageFamilyName))
        {
            return false;
        }

        var clientExePath = processHandle.TryGetProcessImagePath();
        var clientFileName = Path.GetFileName(clientExePath);
        var clientBaseDirectory = Directory.GetParent(Path.GetDirectoryName(clientExePath)!)!;
        var clientBaseDirectoryPath = Path.GetFullPath(clientBaseDirectory.FullName);

        var result = StringComparer.OrdinalIgnoreCase.Equals(clientBaseDirectoryPath, PackageHelper.AppBasePath) &&
                     ExpectedPackagedExeNames.Contains(clientFileName);

        authenticationLevel = result ? IpcAuthenticationLevel.Authenticated : IpcAuthenticationLevel.Anonymous;
        return result;
    }
}
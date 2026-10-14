using FluentBitwarden.Platform.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;
using System.IO.Pipes;
using Windows.ApplicationModel;

namespace FluentBitwarden.Platform.Ipc.Services;

internal sealed class PipeClientsVerifier(ILogger<PipeClientsVerifier> logger) : IIpcClientsVerifier
{
    private static readonly string[] ExpectedPackagedExeNames =
    [
        "FluentBitwarden.ComServer.exe",
        "FluentBitwarden.Ui.exe",
        "FluentBitwarden.AppHost.exe",
        "FluentBitwarden.BrowseProxy.exe",
        "FluentBitwarden.CommandPalette.exe",
    ];
    public IpcAuthenticationLevel IsExpectedClient(NamedPipeServerStream pipe)
    {
        if (!pipe.IsConnected)
            return IpcAuthenticationLevel.Rejected;

        var processId = pipe.GetClientProcessId();
        using var processHandle = IpcPipeExtensions.OpenClientProcess(processId);
        if (processHandle.IsInvalid)
        {
            logger.ClientProcessOpenFailed(processId);
            return IpcAuthenticationLevel.Rejected;
        }

        var clientPackageFamilyName = processHandle.TryGetPackageFamilyName();
        var expectedPackageFamilyName = Package.Current.Id.FamilyName;
        bool isExpectedPackage = StringComparer.OrdinalIgnoreCase.Equals(clientPackageFamilyName, expectedPackageFamilyName);

        var clientExePath = processHandle.TryGetProcessImagePath();
        var clientFileName = Path.GetFileName(clientExePath);
        var clientBaseDirectory = Directory.GetParent(Path.GetDirectoryName(clientExePath)!)!;
        var clientBaseDirectoryPath = Path.GetFullPath(clientBaseDirectory.FullName);

        var isClientFromSameDirectory = StringComparer.OrdinalIgnoreCase.Equals(clientBaseDirectoryPath, PackageHelper.AppBasePath) &&
                                        ExpectedPackagedExeNames.Contains(clientFileName);

        var authenticationLevel = isClientFromSameDirectory && isExpectedPackage
            ? IpcAuthenticationLevel.SamePackage
            : IpcAuthenticationLevel.PackagedExternalProxy;

        return authenticationLevel;
    }
}

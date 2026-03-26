using Windows.ApplicationModel;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace FluentBitwarden.Shared.Extensions;

public static class PackageHelper
{
    private static bool? _isPackaged;

    /// <summary>
    /// Returns true if the app is running with package identity (MSIX packaged).
    /// Returns false for unpackaged / sparse-manifest deployments.
    /// </summary>
    public static bool IsPackaged => _isPackaged ??= CheckIsPackaged();


    private static bool CheckIsPackaged()
    {
        uint length = 0;
        WIN32_ERROR result = PInvoke.GetCurrentPackageFullName(ref length, null);
        return result != WIN32_ERROR.APPMODEL_ERROR_NO_PACKAGE;
    }
}

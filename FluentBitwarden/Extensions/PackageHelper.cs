using Windows.ApplicationModel;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace FluentBitwarden.Extensions;

public static class PackageHelper
{
    private static bool? _isPackaged;
    private static string? _packageRootPath;

    /// <summary>
    /// Returns true if the app is running with package identity (MSIX packaged).
    /// Returns false for unpackaged / sparse-manifest deployments.
    /// </summary>
    public static bool IsPackaged => _isPackaged ??= CheckIsPackaged();

    /// <summary>
    /// Returns the package root directory.
    /// - Packaged:   the MSIX install root  (e.g. C:\Program Files\WindowsApps\MyApp_1.0.0.0_x64__xyz\)
    /// - Unpackaged: the executable directory (e.g. C:\Users\...\AppName\)
    /// </summary>
    public static string PackageRootPath => _packageRootPath ??= ResolvePackageRootPath();


    private static bool CheckIsPackaged()
    {
        uint length = 0;
        WIN32_ERROR result = PInvoke.GetCurrentPackageFullName(ref length, null);
        return result != WIN32_ERROR.APPMODEL_ERROR_NO_PACKAGE;
    }

    private static string ResolvePackageRootPath() =>
        IsPackaged ? Package.Current.InstalledLocation.Path : AppContext.BaseDirectory;
}

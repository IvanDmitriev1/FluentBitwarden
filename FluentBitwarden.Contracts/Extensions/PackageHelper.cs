using Windows.ApplicationModel;

namespace FluentBitwarden.Contracts.Extensions;

public static class PackageHelper
{
    private static bool? _isPackaged;
    private static string? _appBasePath;

    /// <summary>
    /// Returns true if the app is running with package identity (MSIX packaged).
    /// Returns false for unpackaged / sparse-manifest deployments.
    /// </summary>
    public static bool IsPackaged => _isPackaged ??= CheckIsPackaged();

    public static string AppBasePath => _appBasePath ??= ResolvePackageRootPath();

    private static bool CheckIsPackaged()
    {
        uint length = 0;
        WIN32_ERROR result = PInvoke.GetCurrentPackageFullName(ref length, null);
        return result != WIN32_ERROR.APPMODEL_ERROR_NO_PACKAGE;
    }

    private static string ResolvePackageRootPath() =>
        IsPackaged ? Package.Current.InstalledPath : AppContext.BaseDirectory;
}

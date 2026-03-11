using System.Runtime.InteropServices;
using System.Text;
using FluentBitwarden.Core.Abstractions;

namespace FluentBitwarden.Services.Storage;

public sealed class AppPaths : IAppPaths
{
    private const int AppModelErrorNoPackage = 15700;

    public AppPaths()
    {
        IsPackaged = DetectPackaged();
        AppDataRoot = ResolveRoot();
        Directory.CreateDirectory(AppDataRoot);

        VaultDbFilePath = Path.Combine(AppDataRoot, "vault.db");
        SessionFilePath = Path.Combine(AppDataRoot, "session.bin");
        ConfigFilePath = Path.Combine(AppDataRoot, "config.json");
    }

    public bool IsPackaged { get; }
    public string AppDataRoot { get; }
    public string VaultDbFilePath { get; }
    public string SessionFilePath { get; }
    public string ConfigFilePath { get; }

    private string ResolveRoot()
    {
        if (IsPackaged)
        {
            return ResolvePackagedRoot();
        }

        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "FluentBitwarden");
    }

    private static bool DetectPackaged()
    {
        int length = 0;
        int result = GetCurrentPackageFullName(ref length, null);
        return result != AppModelErrorNoPackage;
    }

    private static string ResolvePackagedRoot()
    {
        int length = 0;
        int result = GetCurrentPackageFamilyName(ref length, null);

        if (result == AppModelErrorNoPackage)
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "FluentBitwarden");
        }

        StringBuilder builder = new(length);
        result = GetCurrentPackageFamilyName(ref length, builder);
        if (result != 0)
        {
            throw new InvalidOperationException($"GetCurrentPackageFamilyName failed with error code {result}.");
        }

        string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(root, "Packages", builder.ToString(), "LocalState");
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, StringBuilder? packageFullName);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetCurrentPackageFamilyName(ref int packageFamilyNameLength, StringBuilder? packageFamilyName);
}

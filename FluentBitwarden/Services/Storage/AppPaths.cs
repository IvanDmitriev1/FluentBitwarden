using System.Runtime.InteropServices;
using System.Text;
using Windows.Win32;
using Windows.Win32.Foundation;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Extensions;

namespace FluentBitwarden.Services.Storage;

public sealed class AppPaths : IAppPaths
{
    public AppPaths()
    {
        IsPackaged = PackageHelper.IsPackaged;
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

    private static string ResolvePackagedRoot()
    {
        if (PackageHelper.IsPackaged)
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "FluentBitwarden");
        }

        return Windows.Storage.ApplicationData.Current.LocalFolder.Path;
    }
}

using FluentBitwarden.Abstractions;
using FluentBitwarden.Extensions;

namespace FluentBitwarden.Services.Storage;

public sealed class AppPaths : IAppPaths
{
    public AppPaths()
    {
        AppDataRoot = ResolveRoot();
        Directory.CreateDirectory(AppDataRoot);

        VaultDbFilePath = Path.Combine(AppDataRoot, "vault.db");
        SessionFilePath = Path.Combine(AppDataRoot, "session.bin");
        ConfigFilePath = Path.Combine(AppDataRoot, "config.json");
    }

    public string AppDataRoot { get; }
    public string VaultDbFilePath { get; }
    public string SessionFilePath { get; }
    public string ConfigFilePath { get; }

    private string ResolveRoot()
    {
        if (PackageHelper.IsPackaged)
        {
            return Windows.Storage.ApplicationData.Current.LocalFolder.Path;
        }

        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "FluentBitwarden");
    }
}

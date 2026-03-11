namespace FluentBitwarden.Core.Abstractions;

/// <summary>
/// Provides the file system locations used by FluentBitwarden storage services.
/// </summary>
public interface IAppPaths
{
    /// <summary>
    /// Indicates whether the app is running as a packaged application.
    /// </summary>
    bool IsPackaged { get; }

    /// <summary>
    /// Gets the root directory for app-managed data files.
    /// </summary>
    string AppDataRoot { get; }

    /// <summary>
    /// Gets the path to the encrypted local vault database.
    /// </summary>
    string VaultDbFilePath { get; }

    /// <summary>
    /// Gets the path to the persisted session file.
    /// </summary>
    string SessionFilePath { get; }

    /// <summary>
    /// Gets the path to the persisted local unlock state file.
    /// </summary>
    string UnlockStateFilePath { get; }

    /// <summary>
    /// Gets the path to the app configuration file.
    /// </summary>
    string ConfigFilePath { get; }
}

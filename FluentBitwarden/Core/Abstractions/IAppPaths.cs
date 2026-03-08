namespace FluentBitwarden.Core.Abstractions;

public interface IAppPaths
{
    bool IsPackaged { get; }
    string AppDataRoot { get; }
    string VaultDbFilePath { get; }
    string SessionFilePath { get; }
    string ConfigFilePath { get; }
}

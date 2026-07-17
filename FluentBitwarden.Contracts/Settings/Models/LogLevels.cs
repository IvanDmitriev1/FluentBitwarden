namespace FluentBitwarden.Contracts.Settings.Models;

[Flags]
public enum LogLevels
{
    None = 0,
    Error = 1,
    Warning = 2,
    Information = 4,
    Trace = 8,
}

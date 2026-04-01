namespace FluentBitwarden.Modules.Security.Models;

internal enum WindowsHelloAvailability : byte
{
    NotSupported = 0,
    Available = 0,
    Disabled,
    NotConfigured,
    Unavailable,
}

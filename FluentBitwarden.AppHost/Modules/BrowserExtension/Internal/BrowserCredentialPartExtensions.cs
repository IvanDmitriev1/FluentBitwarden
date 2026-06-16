using FluentBitwarden.Contracts.Modules.BrowserExtension.Models;

namespace FluentBitwarden.AppHost.Modules.BrowserExtension.Internal;

internal static class BrowserCredentialPartExtensions
{
    public static bool Includes(this BrowserCredentialPart value, BrowserCredentialPart flag) =>
        (value & flag) == flag;
}

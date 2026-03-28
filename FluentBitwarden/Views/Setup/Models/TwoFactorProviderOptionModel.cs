using BitwardenApi.Modules.Identity.Models;

namespace FluentBitwarden.Views.Setup.Models;

public sealed record TwoFactorProviderOptionModel(
    TwoFactorProviderType Provider,
    string Title,
    string Subtitle,
    bool IsSupported);
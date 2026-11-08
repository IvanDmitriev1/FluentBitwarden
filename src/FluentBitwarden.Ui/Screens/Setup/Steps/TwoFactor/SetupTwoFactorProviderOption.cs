using BitwardenApi.Identity.Models;

namespace FluentBitwarden.Ui.Screens.Setup.Steps.TwoFactor;

public sealed record SetupTwoFactorProviderOption(IdentityTwoFactorProviderType Provider, string Title);

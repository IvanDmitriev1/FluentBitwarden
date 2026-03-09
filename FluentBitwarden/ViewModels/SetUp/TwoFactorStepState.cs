using BitwaredApi.Models.Auth;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Ui.Controls;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.Json;

namespace FluentBitwarden.ViewModels.SetUp;

public sealed record TwoFactorProviderOptionModel(
    TwoFactorProviderType Provider,
    string Title,
    string Subtitle,
    bool IsSupported);

public partial class TwoFactorStepState(SetupPageViewModel parentViewModel) : ObservableValidator
{
    private const string DefaultPrompt = "Complete the Bitwarden two-factor challenge to continue.";

    public SetupPageViewModel ParentViewModel { get; } = parentViewModel;

    [ObservableProperty]
    public partial TwoFactorProviderOptionModel? SelectedProvider { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Enter the verification code.")]
    public partial string Code { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool RememberThisDevice { get; set; } = true;

    [ObservableProperty]
    public partial string PromptText { get; set; } = DefaultPrompt;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasEmailHint))]
    public partial string EmailHint { get; set; } = string.Empty;

    [ObservableProperty]
    public partial TwoFactorProviderOptionModel[] Providers { get; set; } = [];

    public bool HasEmailHint => !string.IsNullOrWhiteSpace(EmailHint);

    public ValidatableProperty CodeValidation
        => field ??= ValidatableProperty.Create(this, static state => state.Code);

    public void Load(TwoFactorChallenge challenge)
    {
        Providers = challenge.Providers
            .Select(static provider => new TwoFactorProviderOptionModel(
                provider.Provider,
                GetTitle(provider.Provider),
                BuildSubtitle(provider),
                IsSupported(provider.Provider)))
            .ToArray();

        SelectedProvider = Providers.First(provider => provider.IsSupported);
        Debug.Assert(SelectedProvider != null, "No supported two-factor provider found.");

        PromptText = DefaultPrompt;
        EmailHint = challenge.Email ?? string.Empty;
        Code = string.Empty;
        RememberThisDevice = true;
    }

    [RelayCommand]
    private void BackFromTwoFactor()
    {
        ParentViewModel.CurrentStep = SetupPageViewModel.SetupStep.PasswordSignIn;
    }

    public static bool IsSupported(TwoFactorProviderType provider) =>
        provider is TwoFactorProviderType.Authenticator
            or TwoFactorProviderType.Email
            or TwoFactorProviderType.RecoveryCode;

    public bool TryValidateForSubmit()
    {
        ValidateAllProperties();
        return !HasErrors;
    }

    private static string GetTitle(TwoFactorProviderType provider) => provider switch
    {
        TwoFactorProviderType.Authenticator => "Authenticator app",
        TwoFactorProviderType.Email => "Email code",
        TwoFactorProviderType.Duo => "Duo",
        TwoFactorProviderType.Yubikey => "YubiKey",
        TwoFactorProviderType.U2f => "U2F",
        TwoFactorProviderType.WebAuthn => "WebAuthn",
        TwoFactorProviderType.RecoveryCode => "Recovery code",
        _ => provider.ToString(),
    };

    private static string BuildSubtitle(TwoFactorProviderOption provider)
    {
        if (provider.Provider == TwoFactorProviderType.Email
            && TryGetMetadataDisplayValue(provider, out string? emailHint)
            && !string.IsNullOrWhiteSpace(emailHint))
        {
            return emailHint;
        }

        return IsSupported(provider.Provider)
            ? "Supported in this build"
            : "Not supported in this build";
    }

    private static bool TryGetMetadataDisplayValue(TwoFactorProviderOption provider, out string? value)
    {
        if (!provider.Metadata.TryGetValue("Email", out JsonElement metadataElement)
            && !provider.Metadata.TryGetValue("email", out metadataElement))
        {
            value = null;
            return false;
        }

        value = metadataElement.ValueKind switch
        {
            JsonValueKind.String => metadataElement.GetString(),
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => metadataElement.ToString(),
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => metadataElement.ToString(),
        };

        return true;
    }
}

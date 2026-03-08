using System.Text.Json;
using BitwaredApi.Abstractions.Exceptions;
using BitwaredApi.Models.Auth;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Abstractions;

namespace FluentBitwarden.ViewModels.SetUp;

public sealed record TwoFactorProviderOptionModel(
    TwoFactorProviderType Provider,
    string Title,
    string Subtitle,
    bool IsSupported);

public partial class TwoFactorStepViewModel : ObservableObject
{
    private const string DefaultPrompt = "Complete the Bitwarden two-factor challenge to continue.";
    private readonly IAuthService _authService;

    public TwoFactorStepViewModel(
        SetupPageViewModel parentViewModel,
        IAuthService authService)
    {
        ArgumentNullException.ThrowIfNull(parentViewModel);
        ParentViewModel = parentViewModel;
        _authService = authService;
    }

    public SetupPageViewModel ParentViewModel { get; }

    [ObservableProperty]
    public partial TwoFactorProviderOptionModel? SelectedProvider { get; set; }

    [ObservableProperty]
    public partial string Code { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool RememberThisDevice { get; set; } = true;

    [ObservableProperty]
    public partial string PromptText { get; set; } = DefaultPrompt;

    [ObservableProperty]
    public partial string EmailHint { get; set; } = string.Empty;

    [ObservableProperty]
    public partial TwoFactorProviderOptionModel[] Providers { get; set; } = [];

    public bool HasEmailHint => !string.IsNullOrWhiteSpace(EmailHint);

    partial void OnEmailHintChanged(string value)
        => OnPropertyChanged(nameof(HasEmailHint));

    public void LoadChallenge(TwoFactorChallenge challenge)
    {
        Providers = challenge.Providers
            .Select(provider => new TwoFactorProviderOptionModel(
                provider.Provider,
                GetTitle(provider.Provider),
                BuildSubtitle(provider),
                IsSupported(provider.Provider)))
            .ToArray();

        SelectedProvider = Providers.FirstOrDefault(provider => provider.IsSupported) ?? Providers.FirstOrDefault();
        PromptText = DefaultPrompt;
        EmailHint = challenge.Email ?? string.Empty;
        Code = string.Empty;
        RememberThisDevice = true;
    }

    public void Reset()
    {
        SelectedProvider = null;
        Providers = [];
        PromptText = DefaultPrompt;
        EmailHint = string.Empty;
        Code = string.Empty;
        RememberThisDevice = true;
    }

    [RelayCommand]
    private void BackFromTwoFactor()
    {
        ParentViewModel.ClearStatus();
        ParentViewModel.ResetToPasswordStep();
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task ContinueTwoFactorAsync()
    {
        ParentViewModel.ClearStatus();

        if (SelectedProvider is null)
        {
            ParentViewModel.ShowError("Select a two-factor provider.");
            return;
        }

        if (!SelectedProvider.IsSupported)
        {
            ParentViewModel.ShowError("This provider requires an interactive Bitwarden flow that is not implemented in this build.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Code))
        {
            ParentViewModel.ShowError("Enter the verification code.");
            return;
        }

        try
        {
            await ParentViewModel.RunBusyAsync(async () =>
            {
                await _authService.ContinueTwoFactorAsync(
                    Code.Trim(),
                    SelectedProvider.Provider,
                    RememberThisDevice);
                await ParentViewModel.CompleteAuthenticatedSessionAsync();
            });
        }
        catch (Exception ex)
        {
            ParentViewModel.ShowError(ex);
        }
    }

    public static bool IsSupported(TwoFactorProviderType provider) =>
        provider is TwoFactorProviderType.Authenticator
            or TwoFactorProviderType.Email
            or TwoFactorProviderType.RecoveryCode;

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
            && TryGetMetadataDisplayValue(provider, out var emailHint)
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

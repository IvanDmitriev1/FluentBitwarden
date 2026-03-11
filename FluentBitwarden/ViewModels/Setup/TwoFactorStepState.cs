using BitwaredApi;
using BitwaredApi.Abstractions;
using BitwaredApi.Models.Auth;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Ui.Controls;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace FluentBitwarden.ViewModels.Setup;

public sealed record TwoFactorProviderOptionModel(
    TwoFactorProviderType Provider,
    string Title,
    string Subtitle,
    bool IsSupported);

public partial class TwoFactorStepState : ObservableValidator, IDisposable
{
    private const string DefaultPrompt = "Complete the Bitwarden two-factor challenge to continue.";

    private readonly SetupPageViewModel _shell;
    private readonly IAuthenticationWorkflow _authenticationWorkflow;
    private PasswordSignInOutcome.TwoFactorRequired? _twoFactor;

    internal TwoFactorStepState(
        SetupPageViewModel shell,
        IAuthenticationWorkflow authenticationWorkflow)
    {
        _shell = shell;
        _authenticationWorkflow = authenticationWorkflow;
    }

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

    public void Begin(PasswordSignInOutcome.TwoFactorRequired outcome)
    {
        if (ReferenceEquals(_twoFactor, outcome))
        {
            return;
        }

        Clear();
        _twoFactor = outcome;

        Providers = _twoFactor.Challenge.Providers
            .Select(static provider => new TwoFactorProviderOptionModel(
                provider.Provider,
                GetTitle(provider.Provider),
                BuildSubtitle(provider),
                IsSupported(provider.Provider)))
            .ToArray();

        SelectedProvider = Providers.FirstOrDefault(provider => provider.IsSupported);
        if (SelectedProvider is null)
        {
            _shell.ShowError(
                "None of the available two-factor providers are supported in this build. Please use a different device or contact support for assistance.");
            return;
        }

        PromptText = DefaultPrompt;
        EmailHint = _twoFactor.Challenge.Email ?? string.Empty;
    }

    public void Clear()
    {
        _twoFactor?.Continuation.Dispose();
        _twoFactor = null;
        Providers = [];
        SelectedProvider = null;
        Code = string.Empty;
        RememberThisDevice = true;
        PromptText = DefaultPrompt;
        EmailHint = string.Empty;
        ClearErrors();
    }

    public void Dispose() => Clear();


    [RelayCommand]
    private void BackFromTwoFactor()
    {
        _shell.ShowPasswordStep();
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task ContinueTwoFactorAsync()
    {
        if (!TryValidateForSubmit())
        {
            return;
        }

        if (_twoFactor is null)
        {
            Clear();
            _shell.ShowError("The two-factor session has expired. Sign in again.");
            _shell.ShowPasswordStep();
            return;
        }

        BitwardenClientContext context = _shell.FlowContext.ClientContext;

        TwoFactorProviderOptionModel selectedProvider = SelectedProvider
            ?? throw new InvalidOperationException("No two-factor provider is selected.");

        try
        {
            _shell.IsBusy = true;

            AuthenticationOutcome authenticationOutcome = await _authenticationWorkflow
                .ContinueTwoFactorAsync(
                    new TwoFactorSignInRequest(
                        context,
                        _twoFactor.Continuation,
                        Code.Trim(),
                        selectedProvider.Provider,
                        RememberThisDevice))
                .ConfigureAwait(true);

            switch (authenticationOutcome)
            {
                case AuthenticationOutcome.Success success:
                    await _shell.CompleteAuthenticatedSessionAsync(success.Authentication).ConfigureAwait(true);
                    break;

                case AuthenticationOutcome.InvalidCredentials invalidCredentials:
                    _shell.ShowError(invalidCredentials.Message);
                    break;

                case AuthenticationOutcome.DeviceVerificationRequired deviceVerificationRequired:
                    _shell.ShowError(deviceVerificationRequired.Message);
                    break;

                default:
                    throw new InvalidOperationException("Unsupported two-factor authentication outcome.");
            }
        }
        catch (Exception ex)
        {
            _shell.ShowError(ex);
        }
        finally
        {
            _shell.IsBusy = false;
        }
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

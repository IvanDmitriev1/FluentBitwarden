using BitwardenApi.Modules.Identity.Models;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Resources.Controls;
using FluentBitwarden.Views.Setup.Models;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BitwardenApi.Shared.Context;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Models;

namespace FluentBitwarden.Views.Setup.States;

public partial class TwoFactorStepState : ObservableValidator
{
    private readonly SetupLoginContext _context;
    private readonly IAccountSessionManager _accountSessionManager;
    private readonly Action _onSuccess;
    private readonly string _email;
    private readonly string _serverAuthorizationHash;

    public TwoFactorStepState(
        SetupLoginContext context,
        AccountSignInOutcome.TwoFactorRequired twoFactorRequired,
        IAccountSessionManager accountSessionManager,
        Action onSuccess)
    {
        _context = context;
        _accountSessionManager = accountSessionManager;
        _onSuccess = onSuccess;
        _email = twoFactorRequired.Email;
        _serverAuthorizationHash = twoFactorRequired.ServerAuthorizationHash;

        Providers = twoFactorRequired.Challenge.Providers
            .Select(static provider => new TwoFactorProviderOptionModel(
                provider.Provider,
                provider.Provider.GetTitle(),
                BuildSubtitle(provider),
                IsSupported(provider.Provider)))
            .ToArray();

        SelectedProvider = Providers[0];
    }

    [ObservableProperty]
    public partial TwoFactorProviderOptionModel SelectedProvider { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Enter the verification code.")]
    [CustomValidation(typeof(TwoFactorStepState), nameof(ValidateCode))]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification = "Generated setter delegates to ObservableValidator.ValidateProperty, which is intentionally preserved for this trim-aware validation path.")]
    public partial string Code { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasInvalidCode { get; set; }

    [field: MaybeNull]
    public ValidatableProperty CodeValidation
        => field ??= ValidatableProperty.Create(this, static state => state.Code);

    public TwoFactorProviderOptionModel[] Providers { get; }

    public static ValidationResult? ValidateCode(string code, ValidationContext context)
    {
        TwoFactorStepState instance = (TwoFactorStepState)context.ObjectInstance;

        return !instance.HasInvalidCode
            ? ValidationResult.Success
            : new ValidationResult("Invalid verification code.");
    }

    partial void OnHasInvalidCodeChanged(bool value)
    {
        ValidateAllProperties();
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task ContinueTwoFactorAsync()
    {
        HasInvalidCode = false;

        ValidateAllProperties();
        if (HasErrors)
            return;

        var result = await _accountSessionManager.SignInAsync(new AccountSignInRequest.TwoFactorRequest(_context.BitwardenClientContext,
            _email,
            _serverAuthorizationHash,
            new TwoFactorProof(Code, SelectedProvider.Provider)), CancellationToken.None);

        switch (result)
        {
            case AccountSignInOutcome.Success:
                _onSuccess.Invoke();
                return;

            case AccountSignInOutcome.InvalidCredentials:
            case AccountSignInOutcome.DeviceVerificationRequired:
                HasInvalidCode = true;
                return;

            default:
                HasInvalidCode = true;
                return;
        }
    }

    public static bool IsSupported(TwoFactorProviderType provider) =>
        provider is TwoFactorProviderType.Authenticator or TwoFactorProviderType.Email;

    private static string BuildSubtitle(TwoFactorProviderOption provider)
    {
        if (provider.Provider == TwoFactorProviderType.Email
            && provider.TryGetMetadataDisplayValue(out string? emailHint)
            && !string.IsNullOrWhiteSpace(emailHint))
        {
            return emailHint;
        }

        return IsSupported(provider.Provider)
            ? "Supported in this build"
            : "Not supported in this build";
    }
}

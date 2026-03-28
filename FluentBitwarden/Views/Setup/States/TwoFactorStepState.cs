using BitwardenApi.Modules.Identity.Models;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Models.Authentication;
using FluentBitwarden.Resources.Controls;
using FluentBitwarden.Views.Setup.Models;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;

namespace FluentBitwarden.Views.Setup.States;

public partial class TwoFactorStepState : ObservableValidator
{
    private readonly SetupLoginContext _context;
    private readonly IAuthenticationService _authenticationService;
    private readonly PasswordSignInContinuation _loginContinuation;

    public TwoFactorStepState(
        SetupLoginContext context,
        PasswordSignInOutcome.TwoFactorRequired twoFactorRequired,
        IAuthenticationService authenticationService)
    {
        _context = context;
        _authenticationService = authenticationService;
        _loginContinuation = twoFactorRequired.Continuation;

        Providers = twoFactorRequired.Challenge.Providers
            .Select(static provider => new TwoFactorProviderOptionModel(
                provider.Provider,
                provider.Provider.GetTitle(),
                BuildSubtitle(provider),
                IsSupported(provider.Provider)))
            .ToArray();
    }

    [ObservableProperty]
    public partial TwoFactorProviderOptionModel? SelectedProvider { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Enter the verification code.")]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification = "Generated setter delegates to ObservableValidator.ValidateProperty, which is intentionally preserved for this trim-aware validation path.")]
    public partial string Code { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PromptText { get; set; } = "Complete the Bitwarden two-factor challenge to continue";

    [field: MaybeNull]
    public ValidatableProperty CodeValidation
        => field ??= ValidatableProperty.Create(this, static state => state.Code);

    public TwoFactorProviderOptionModel[] Providers { get; }


    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task ContinueTwoFactorAsync()
    {
        ValidateAllProperties();
        if (HasErrors || SelectedProvider is null)
            return;

        var result = await _authenticationService.ContinueTwoFactorAsync(
            _context.BitwardenClientContext,
            _loginContinuation,
            new TwoFactorProof(Code, SelectedProvider.Provider), CancellationToken.None);


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
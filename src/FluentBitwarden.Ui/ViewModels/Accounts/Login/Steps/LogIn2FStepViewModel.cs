using CommunityToolkit.Mvvm.Input;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using FluentBitwarden.Contracts.Modules.Accounts.Authentication;

namespace FluentBitwarden.ViewModels.Accounts.Login.Steps;

[Microsoft.UI.Xaml.Data.Bindable]
internal sealed partial class LogIn2FStepViewModel : ObservableValidatorEx
{
    public LogIn2FStepViewModel(
        AccountAuthenticationOutcome.TwoFactorRequired twoFactorRequired,
        LogInFlowPageViewModel flow)
    {
        _twoFactorRequired = twoFactorRequired;
        _flow = flow;

        Providers = twoFactorRequired.Challenge.Providers
            .Select(LogIn2FProviderOptionModel.CreateFrom)
            .ToArray();

        SelectedProvider = Providers[0];

        Email = flow.Context.Email;
        ServerDisplayName = flow.Context.BitwardenContext.Environment.ToServerDisplayName();
    }

    private readonly AccountAuthenticationOutcome.TwoFactorRequired _twoFactorRequired;
    private readonly LogInFlowPageViewModel _flow;

    public string Email { get; }
    public string ServerDisplayName { get; }

    public LogIn2FProviderOptionModel[] Providers { get; }

    [ObservableProperty]
    public partial LogIn2FProviderOptionModel SelectedProvider { get; set; }


    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Enter the verification code.")]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification = "Generated setter delegates to ObservableValidator.ValidateProperty, which is intentionally preserved for this trim-aware validation path.")]
    public partial string Code { get; set; } = string.Empty;


    [field: MaybeNull]
    public ValidatableProperty CodeValidation
        => field ??= ValidatableProperty.Create(this,
            static state => state.Code);


    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task LogIn2FAsync(string? password)
    {
        ClearAllManualErrors();
        ValidateAllProperties();

        if (HasErrors)
            return;

        var context = _flow.Context;

        var outcome = await _flow.AccountsClient.AuthenticateAsync(new AccountAuthenticationRequest.TwoFactor(
            context.BitwardenContext,
            _twoFactorRequired.Email,
            _twoFactorRequired.ServerAuthorizationHash,
            new IdentityTwoFactorProof(Code, SelectedProvider.Provider)), CancellationToken.None);

        switch (outcome)
        {
            case AccountAuthenticationOutcome.Success success:
                await _flow.OnSuccessLogIn(success.Account);
                return;

            case AccountAuthenticationOutcome.InvalidCredentials invalidCredentials:
                SetError(nameof(Code), invalidCredentials.Message);
                return;

            default:
                throw new InvalidOperationException("Unsupported two-factor sign-in outcome.");
        }
    }
}

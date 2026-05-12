using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Modules.Session.Models;
using FluentBitwarden.Resources;
using FluentBitwarden.UI.Controls;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using FluentBitwarden.Infrastructure.Extensions;

namespace FluentBitwarden.Views.LogIn.States;

internal sealed partial class LogInPasswordStepViewModel(LogInFlowPageViewModel flow) : ObservableValidatorEx
{
    public string Email { get; } = flow.Context.Email;
    public string ServerDisplayName { get; } = flow.Context.BitwardenContext.Environment.ToServerDisplayName();

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [StringLength(int.MaxValue, MinimumLength = 8, ErrorMessage = "Master password must be at least 8 characters long.")]
    [Required(ErrorMessage = "Enter your master password.")]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification = "Generated setter delegates to ObservableValidator.ValidateProperty, which is intentionally preserved for this trim-aware validation path.")]
    public partial string MasterPassword { get; set; } = string.Empty;


    [field: AllowNull]
    public ValidatableProperty MasterPasswordValidation
        => field ??= ValidatableProperty.Create(this,
            static x => x.MasterPassword);



    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task LogInWithPasswordAsync(string password)
    {
        ClearAllManualErrors();
        MasterPassword = password;

        if (HasErrors)
        {
            return;
        }

        var outcome = await flow.AccountSessionManager.SignInAsync(
            new AccountLoginRequest.PasswordRequest(flow.Context.BitwardenContext, flow.Context.Email, MasterPassword),
            CancellationToken.None);

        switch (outcome)
        {
            case AccountLoginnOutcome.Success success:
                flow.OnSuccessLogIn(success.AccountSignInSuccess);
                return;
            case AccountLoginnOutcome.InvalidCredentials e:
                SetError(nameof(MasterPassword), e.Message);
                return;

            case AccountLoginnOutcome.TwoFactorRequired twoFactorRequired:
                flow.Show2FStep(twoFactorRequired);
                return;
            default:
                throw new InvalidOperationException("Unsupported password sign-in outcome.");
        }
    }
}
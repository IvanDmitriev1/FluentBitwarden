using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Resources.Controls;
using FluentBitwarden.Views.LogIn.Models;
using FluentBitwarden.Views.LogIn.ValidationAttributes;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using BitwardenApi.Shared.Context;

namespace FluentBitwarden.Views.LogIn.States;

public sealed partial class LogInEmailStepViewModel : ObservableValidator
{
    public LogInEmailStepViewModel(LogInFlowPageViewModel flow)
    {
        _flow = flow;

        Environments =
        [
            LogInEnvironmentOption.Us,
            LogInEnvironmentOption.Eu,
            new LogInEnvironmentOption("Custom", string.Empty),
        ];

        SelectedEnvironment = Environments[0];
    }

    private readonly LogInFlowPageViewModel _flow;

    public LogInEnvironmentOption[] Environments { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCustomEnvironmentSelected))]
    public partial LogInEnvironmentOption? SelectedEnvironment { get; set; }

    public bool IsCustomEnvironmentSelected => SelectedEnvironment?.Title == "Custom";

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Required(ErrorMessage = "Enter your Bitwarden email address.")]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification =
            "Generated setter delegates to ObservableValidator.ValidateProperty, which is intentionally preserved for this trim-aware validation path.")]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomServerUrl<LogInEmailStepViewModel>(shouldValidateMemberName:nameof(IsCustomEnvironmentSelected))]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification = "Generated setter delegates to ObservableValidator.ValidateProperty, which is intentionally preserved for this trim-aware validation path.")]
    public partial string CustomServerUrl { get; set; } = string.Empty;


    [field: AllowNull]
    public ValidatableProperty EmailValidation
        => field ??= ValidatableProperty.Create(this,
            static x => x.Email);

    [field: AllowNull]
    public ValidatableProperty CustomServerUrlValidation
        => field ??= ValidatableProperty.Create(this,
            static x => x.CustomServerUrl);


    [RelayCommand]
    private void Continue()
    {
        ValidateAllProperties();
        if (HasErrors || SelectedEnvironment is null)
            return;

        BitwardenEnvironment environment;
        if (SelectedEnvironment == LogInEnvironmentOption.Eu)
        {
            environment = BitwardenEnvironment.Europe;
        }
        else if (SelectedEnvironment == LogInEnvironmentOption.Us)
        {
            environment = BitwardenEnvironment.UnitedStates;
        }
        else
        {
            environment = new BitwardenEnvironment(new Uri($"{CustomServerUrl}/api"),
                new Uri($"{CustomServerUrl}/identity"), new Uri($"{CustomServerUrl}/notifications"));
        }

        _flow.Context.Email = Email;
        _flow.Context.ChangeEnvironment(environment);
        _flow.ShowPasswordStep();
    }
}
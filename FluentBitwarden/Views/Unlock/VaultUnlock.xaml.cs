using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Modules.Account.Models;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Models;
using FluentBitwarden.Resources.Controls;
using FluentBitwarden.Resources.UserControls;
using Microsoft.UI.Xaml;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;

namespace FluentBitwarden.Views.Unlock;

[DependencyProperty<AccountProfile>("Account")]
[DependencyProperty<ICommand>("ResultCommand")]
public sealed partial class VaultUnlock : ValidatingUserControl
{
    public VaultUnlock()
    {
        InitializeComponent();

        _accountSessionManager = App.Current.GetRequiredService<IAccountSessionManager>();
    }

    private readonly IAccountSessionManager _accountSessionManager;

    public string Password => PasswordBox.Password;

    [field: AllowNull]
    public ValidatableProperty PasswordValidation
        => field ??= ValidatableProperty.Create(this, static state => state.Password);

    [RelayCommand]
    private void Unlock()
    {
        ClearError(nameof(Password));

        if (Account is null)
        {
            SetError(nameof(Password), "No account selected.");
            return;
        }

        if (!ValidateRequired(
                nameof(Password),
                Password,
                "Enter your master password."))
        {
            PasswordBox.Focus(FocusState.Programmatic);
            return;
        }

        PasswordBox.IsPasswordRevealed = false;
        PasswordBox.IsEnabled = false;
        AccountUnlockOutcome result;

        try
        {
            result = _accountSessionManager.Unlock(new AccountUnlockRequest.MasterPasswordRequest(Account, Password));
        }
        finally
        {
            PasswordBox.IsEnabled = true;
        }

        switch (result)
        {
            case AccountUnlockOutcome.Failure failure:
                SetError(nameof(Password), failure.Reason);
                PasswordBox.Focus(FocusState.Programmatic);
                break;
            default:
                ResultCommand?.Execute(result);
                break;
        }
    }
}
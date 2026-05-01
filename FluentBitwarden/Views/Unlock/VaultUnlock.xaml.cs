using FluentBitwarden.Modules.Account.Models;
using FluentBitwarden.Modules.Security.Abstractions;
using FluentBitwarden.Modules.Security.Models.Unlock;
using FluentBitwarden.Modules.Security.Services.Unlock;
using FluentBitwarden.Resources.Controls;
using FluentBitwarden.Shared.UserControls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;
using Windows.System;

namespace FluentBitwarden.Views.Unlock;

[DependencyProperty<StoredAccount>("Account")]
[DependencyProperty<ICommand>("ResultCommand")]
public sealed partial class VaultUnlock : ValidatingUserControl
{
    public VaultUnlock()
    {
        InitializeComponent();

        _unlockService = App.Current.GetRequiredService<IUnlockService>();
    }

    private readonly IUnlockService _unlockService;
    private bool _isUnlocking;

    public string Password => PasswordBox?.Password ?? string.Empty;

    [field: AllowNull]
    public ValidatableProperty PasswordValidation
        => field ??= ValidatableProperty.Create(this, static state => state.Password);

    private async void PasswordBox_OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Enter)
            return;

        e.Handled = true;
        await UnlockAsync();
    }

    private async Task UnlockAsync()
    {
        if (_isUnlocking)
            return;

        _isUnlocking = true;

        try
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

            var result = await _unlockService.UnlockAsync(
                Account.UserId,
                new MasterPasswordUnlockRequest(Password));

            switch (result)
            {
                case UnlockResult.Failure failure:
                    SetError(nameof(Password), failure.Reason);
                    PasswordBox.Focus(FocusState.Programmatic);
                    break;
                default:
                    ResultCommand?.Execute(result);
                    break;
            }
        }
        finally
        {
            _isUnlocking = false;
        }
    }
}
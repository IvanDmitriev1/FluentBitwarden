using System.Security.Cryptography;
using BitwaredApi.Abstractions;
using BitwaredApi.Abstractions.Exceptions;
using BitwaredApi.Models.Session;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Models;
using FluentBitwarden.Security;
using FluentBitwarden.Ui.Abstractions;
using VaultPage = FluentBitwarden.Views.VaultPage;
using SetupPage = FluentBitwarden.Views.SetUp.SetupPage;

namespace FluentBitwarden.ViewModels;

public partial class LoginPageViewModel(
    IAuthService authService,
    ILocalUnlockService localUnlockService,
    INavigationService navigationService)
    : ObservableObject, IPageLifecycleAware
{
    private StoredSessionInfo? _session;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool HasError { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EnvironmentLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string MasterPassword { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Pin { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool CanUnlockWithMasterPassword { get; set; }

    [ObservableProperty]
    public partial bool CanUnlockWithWindowsHello { get; set; }

    [ObservableProperty]
    public partial bool CanUnlockWithPin { get; set; }

    public bool HasNoUnlockOptions => !CanUnlockWithMasterPassword && !CanUnlockWithWindowsHello && !CanUnlockWithPin;

    partial void OnCanUnlockWithMasterPasswordChanged(bool value) => OnPropertyChanged(nameof(HasNoUnlockOptions));
    partial void OnCanUnlockWithWindowsHelloChanged(bool value) => OnPropertyChanged(nameof(HasNoUnlockOptions));
    partial void OnCanUnlockWithPinChanged(bool value) => OnPropertyChanged(nameof(HasNoUnlockOptions));

    public async Task OnLoadingAsync(CancellationToken cancellationToken)
    {
        ClearError();
        MasterPassword = string.Empty;
        Pin = string.Empty;

        _session = await authService.GetStoredSessionAsync(cancellationToken);
        if (_session is null)
        {
            navigationService.Navigate(typeof(SetupPage), clearBackStack: true);
            return;
        }

        if (!_session.IsLocked)
        {
            navigationService.Navigate(typeof(VaultPage), clearBackStack: true);
            return;
        }

        LocalUnlockStatus status = await localUnlockService.GetStatusAsync(_session.AccountId, cancellationToken);
        Email = _session.Email;
        EnvironmentLabel = DescribeEnvironment(_session);
        CanUnlockWithMasterPassword = _session.CanUnlockWithMasterPassword;
        CanUnlockWithWindowsHello = status is { IsWindowsHelloAvailable: true, IsWindowsHelloEnrolled: true };
        CanUnlockWithPin = status.IsPinEnrolled;
    }

    public Task OnUnloadingAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task UnlockWithMasterPasswordAsync()
    {
        if (_session is null)
        {
            return;
        }

        if (!CanUnlockWithMasterPassword)
        {
            ShowError("This session cannot be unlocked with the master password.");
            return;
        }

        if (string.IsNullOrWhiteSpace(MasterPassword))
        {
            ShowError("Enter your master password.");
            return;
        }

        await ExecuteUnlockAsync(async ct =>
        {
            await authService.UnlockWithMasterPasswordAsync(MasterPassword, ct);
        });
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task UnlockWithWindowsHelloAsync()
    {
        if (_session is null)
        {
            return;
        }

        if (!CanUnlockWithWindowsHello)
        {
            ShowError("Windows Hello unlock is not available.");
            return;
        }

        await ExecuteUnlockAsync(async ct =>
        {
            byte[]? userKey = null;

            try
            {
                userKey = await localUnlockService.UnlockWithWindowsHelloAsync(_session.AccountId, ct);
                await authService.UnlockWithUserKeyAsync(userKey, ct);
            }
            finally
            {
                if (userKey is not null)
                {
                    CryptographicOperations.ZeroMemory(userKey);
                }
            }
        });
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task UnlockWithPinAsync()
    {
        if (_session is null)
        {
            return;
        }

        if (!CanUnlockWithPin)
        {
            ShowError("PIN unlock is not available.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Pin))
        {
            ShowError("Enter your app PIN.");
            return;
        }

        await ExecuteUnlockAsync(async ct =>
        {
            byte[]? userKey = null;

            try
            {
                userKey = await localUnlockService.UnlockWithPinAsync(_session.AccountId, Pin.Trim(), ct);
                await authService.UnlockWithUserKeyAsync(userKey, ct);
            }
            finally
            {
                if (userKey is not null)
                {
                    CryptographicOperations.ZeroMemory(userKey);
                }
            }
        });
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task UseAnotherAccountAsync()
    {
        ClearError();
        IsBusy = true;

        try
        {
            await authService.LogoutAsync();
            await localUnlockService.ClearAsync();
            navigationService.Navigate(typeof(SetupPage), clearBackStack: true);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExecuteUnlockAsync(Func<CancellationToken, Task> action)
    {
        ClearError();
        IsBusy = true;

        try
        {
            await action(CancellationToken.None);
            MasterPassword = string.Empty;
            Pin = string.Empty;
            navigationService.Navigate(typeof(VaultPage), clearBackStack: true);
        }
        catch (InvalidCredentialsException ex)
        {
            ShowError(ex.Message);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string DescribeEnvironment(StoredSessionInfo session)
    {
        string host = session.Environment.ApiBase.Host;

        if (host.Contains("bitwarden.eu", StringComparison.OrdinalIgnoreCase))
        {
            return "Bitwarden EU";
        }

        if (host.Contains("bitwarden.com", StringComparison.OrdinalIgnoreCase))
        {
            return "Bitwarden US";
        }

        return host;
    }

    private void ClearError()
    {
        HasError = false;
        ErrorMessage = string.Empty;
    }

    private void ShowError(string message)
    {
        HasError = true;
        ErrorMessage = message;
    }
}

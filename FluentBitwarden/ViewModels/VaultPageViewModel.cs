using System.Collections.ObjectModel;
using BitwaredApi.Abstractions;
using BitwaredApi.Models.Vault;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Security;
using FluentBitwarden.Ui.Abstractions;
using FluentBitwarden.Views;
using LoginPage = FluentBitwarden.Views.LoginPage;
using SetupPage = FluentBitwarden.Views.SetUp.SetupPage;

namespace FluentBitwarden.ViewModels;

public sealed record VaultCipherRow(
    string Id,
    string Name,
    string Username,
    string Password,
    string Urls,
    bool HasDecryptionError);

public partial class VaultPageViewModel(
    IVaultService vaultService,
    IBitwaredClient client,
    INavigationService navigationService,
    ILocalUnlockService localUnlockService)
    : ObservableObject, IPageLifecycleAware
{
    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool HasError { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public ObservableCollection<VaultCipherRow> Ciphers { get; } = [];

    public async Task OnLoadingAsync(CancellationToken cancellationToken)
    {
        var session = await client.Auth.GetStoredSessionAsync(cancellationToken);
        if (session is null)
        {
            navigationService.Navigate(typeof(SetupPage), clearBackStack: true);
            return;
        }

        if (session.IsLocked)
        {
            navigationService.Navigate(typeof(LoginPage), clearBackStack: true);
            return;
        }

        await RefreshAsync();
    }

    public Task OnUnloadingAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task RefreshAsync()
    {
        ClearError();
        IsBusy = true;

        try
        {
            var session = await client.Auth.GetStoredSessionAsync();
            if (session is null)
            {
                navigationService.Navigate(typeof(SetupPage), clearBackStack: true);
                return;
            }

            if (session.IsLocked)
            {
                navigationService.Navigate(typeof(LoginPage), clearBackStack: true);
                return;
            }

            IReadOnlyList<DecryptedCipher> ciphers = await vaultService.ListCiphersAsync();

            Ciphers.Clear();
            foreach (DecryptedCipher cipher in ciphers)
            {
                Ciphers.Add(new VaultCipherRow(
                    cipher.Id,
                    cipher.Name ?? "(unable to decrypt)",
                    cipher.Username ?? string.Empty,
                    cipher.Password ?? string.Empty,
                    string.Join(", ", cipher.Uris),
                    cipher.HasDecryptionError));
            }
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

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task LockAsync()
    {
        await client.LockAsync();
        navigationService.Navigate(typeof(LoginPage), clearBackStack: true);
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task LogoutAsync()
    {
        await client.LogoutAsync();
        await localUnlockService.ClearAsync();
        navigationService.Navigate(typeof(SetupPage), clearBackStack: true);
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

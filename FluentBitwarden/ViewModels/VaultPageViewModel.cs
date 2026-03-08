using System.Collections.ObjectModel;
using BitwaredApi.Models.Vault;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Abstractions.UnlockServices;
using FluentBitwarden.Models.Navigation;
using FluentBitwarden.Ui.Abstractions;
using FluentBitwarden.Views;
using FluentBitwarden.Views.SetUp;

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
    ILocalVaultUnlocker localVaultUnlocker,
    IUnlockSettingsPromptService unlockSettingsPromptService)
    : ObservableObject, IPageLifecycleAware
{
    private bool _shouldRecommendUnlockSettings;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool HasError { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public ObservableCollection<VaultCipherRow> Ciphers { get; } = [];

    public async Task OnLoadingAsync(CancellationToken cancellationToken)
    {
        await RefreshAsync();

        if (_shouldRecommendUnlockSettings)
        {
            _shouldRecommendUnlockSettings = false;

            bool openSettings = await unlockSettingsPromptService.ShowUnlockSettingsPromptAsync(cancellationToken);
            if (openSettings)
            {
                navigationService.Navigate<SettingsPage>(clearBackStack: true);
            }
        }
    }

    public Task OnUnloadingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task RefreshAsync()
    {
        ClearError();
        IsBusy = true;

        try
        {
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
        await localVaultUnlocker.LockAsync();
        navigationService.Navigate<LoginPage>(clearBackStack: true);
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task LogoutAsync()
    {
        await client.LogoutAsync();
        await localVaultUnlocker.ClearAsync();
        navigationService.Navigate<SetupPage>(clearBackStack: true);
    }

    [RelayCommand]
    private void OpenSettings()
        => navigationService.Navigate<SettingsPage>();

    public void SetNavigationContext(VaultNavigationContext? context)
        => _shouldRecommendUnlockSettings = context?.ShowUnlockSettingsRecommendation == true;

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

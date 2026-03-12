using System.Collections.ObjectModel;
using BitwaredApi.Models.Vault;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Models.Navigation;
using FluentBitwarden.Models.Vault;
using FluentBitwarden.Ui.Abstractions;
using FluentBitwarden.Views;
using FluentBitwarden.Views.Setup;

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
    INavigationService navigationService,
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
        await RefreshAsync().ConfigureAwait(true);

        if (_shouldRecommendUnlockSettings)
        {
            _shouldRecommendUnlockSettings = false;

            bool openSettings = await unlockSettingsPromptService.ShowUnlockSettingsPromptAsync(cancellationToken).ConfigureAwait(true);
            if (openSettings)
            {
                navigationService.Navigate<SettingsPage>(clearBackStack: true);
            }
        }
    }

    public Task OnUnloadingAsync() => Task.CompletedTask;

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task RefreshAsync()
    {
        ClearError();
        IsBusy = true;

        try
        {
            VaultSyncOutcome syncOutcome = await vaultService.SyncAsync().ConfigureAwait(true);
            switch (syncOutcome)
            {
                case VaultSyncOutcome.Success:
                case VaultSyncOutcome.Offline:
                    break;

                case VaultSyncOutcome.Locked:
                    navigationService.Navigate<LoginPage>(clearBackStack: true);
                    return;

                case VaultSyncOutcome.Unavailable:
                    navigationService.Navigate<SetupPage>(clearBackStack: true);
                    return;

                default:
                    throw new InvalidOperationException("Unsupported vault sync outcome.");
            }

            VaultReadOutcome<IReadOnlyList<DecryptedCipher>> readOutcome = await vaultService.ListCiphersAsync().ConfigureAwait(true);
            switch (readOutcome)
            {
                case VaultReadOutcome<IReadOnlyList<DecryptedCipher>>.Success success:
                    PopulateCiphers(success.Value);
                    break;

                case VaultReadOutcome<IReadOnlyList<DecryptedCipher>>.Locked locked:
                    ShowError(locked.Message);
                    navigationService.Navigate<LoginPage>(clearBackStack: true);
                    break;

                case VaultReadOutcome<IReadOnlyList<DecryptedCipher>>.NoCachedData noCachedData:
                    Ciphers.Clear();
                    ShowError(noCachedData.Message);
                    break;

                case VaultReadOutcome<IReadOnlyList<DecryptedCipher>>.Unavailable unavailable:
                    ShowError(unavailable.Message);
                    navigationService.Navigate<SetupPage>(clearBackStack: true);
                    break;

                default:
                    throw new InvalidOperationException("Unsupported vault read outcome.");
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
        await vaultService.LockAsync().ConfigureAwait(true);
        navigationService.Navigate<LoginPage>(clearBackStack: true);
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task LogoutAsync()
    {
        await vaultService.LogoutAsync().ConfigureAwait(true);
        navigationService.Navigate<SetupPage>(clearBackStack: true);
    }

    [RelayCommand]
    private void OpenSettings()
        => navigationService.Navigate<SettingsPage>();

    public void SetNavigationContext(VaultNavigationContext? context)
        => _shouldRecommendUnlockSettings = context?.ShowUnlockSettingsRecommendation == true;

    private void PopulateCiphers(IReadOnlyList<DecryptedCipher> ciphers)
    {
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

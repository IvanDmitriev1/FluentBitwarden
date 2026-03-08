using System.Collections.ObjectModel;
using BitwaredApi.Abstractions;
using BitwaredApi.Models.Vault;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Ui.Abstractions;
using FluentBitwarden.Views;
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
    INavigationService navigationService)
    : ObservableObject, IPageLifecycleAware
{
    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool HasError { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public ObservableCollection<VaultCipherRow> Ciphers { get; } = [];

    public Task OnLoadingAsync(CancellationToken cancellationToken)
        => RefreshAsync();

    public Task OnUnloadingAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

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
    private async Task LogoutAsync()
    {
        await client.LogoutAsync();
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

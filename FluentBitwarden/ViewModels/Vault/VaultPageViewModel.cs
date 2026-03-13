using BitwaredApi.Models.Vault;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Models.Vault;
using FluentBitwarden.Ui.Abstractions;
using FluentBitwarden.Views.Login;
using FluentBitwarden.Views.Setup;
using System.Collections.ObjectModel;

namespace FluentBitwarden.ViewModels.Vault;

public sealed partial class VaultPageViewModel : ObservableObject, IPageLifecycleAware
{
    private readonly IVaultService _vaultService;
    private readonly INavigationService _navigationService;

    public VaultPageViewModel(IVaultService vaultService, INavigationService navigationService)
    {
        _vaultService = vaultService;
        _navigationService = navigationService;
    }

    [ObservableProperty]
    public partial bool IsBusy { get; private set; }

    [ObservableProperty]
    public partial DecryptedCipher? SelectedCipher { get; set; }

    public ObservableCollection<DecryptedCipher> Ciphers { get; } = [];

    public async Task OnLoadingAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;

        try
        {
            var syncOutcome = await _vaultService.SyncAsync(cancellationToken);
            switch (syncOutcome)
            {
                case VaultSyncOutcome.Success:
                case VaultSyncOutcome.Offline:
                    break;

                case VaultSyncOutcome.Locked:
                    _navigationService.Navigate<LoginPage>(clearBackStack: true);
                    return;

                case VaultSyncOutcome.Unavailable:
                    _navigationService.Navigate<SetupPage>(clearBackStack: true);
                    return;

                default:
                    throw new InvalidOperationException("Unsupported vault sync outcome.");
            }

            var readOutcome = await _vaultService.ListCiphersAsync(cancellationToken);
            switch (readOutcome)
            {
                case VaultReadOutcome<IReadOnlyList<DecryptedCipher>>.Success success:
                    foreach (var cipher in success.Value)
                    {
                        Ciphers.Add(cipher);
                    }
                    break;

                case VaultReadOutcome<IReadOnlyList<DecryptedCipher>>.Locked locked:
                    _navigationService.Navigate<LoginPage>(clearBackStack: true);
                    break;

                case VaultReadOutcome<IReadOnlyList<DecryptedCipher>>.NoCachedData noCachedData:
                    Ciphers.Clear();
                    //ShowError(noCachedData.Message);
                    break;

                case VaultReadOutcome<IReadOnlyList<DecryptedCipher>>.Unavailable unavailable:
                    //ShowError(unavailable.Message);
                    _navigationService.Navigate<SetupPage>(clearBackStack: true);
                    break;

                default:
                    throw new InvalidOperationException("Unsupported vault read outcome.");
            }
        }
        catch (Exception ex)
        {
            //ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public Task OnUnloadingAsync()
    {
        return Task.CompletedTask;
    }
}
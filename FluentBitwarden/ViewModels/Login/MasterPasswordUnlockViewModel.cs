using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Abstractions.UnlockServices;
using FluentBitwarden.Models;

namespace FluentBitwarden.ViewModels;

internal partial class MasterPasswordUnlockViewModel : ObservableObject
{
    private readonly IMasterPasswordUnlockService _masterPasswordUnlockService;

    public MasterPasswordUnlockViewModel(
        LoginPageViewModel parentViewModel,
        IMasterPasswordUnlockService masterPasswordUnlockService)
    {
        ParentViewModel = parentViewModel;
        _masterPasswordUnlockService = masterPasswordUnlockService;
        Method = new LoginUnlockMethodItem(
            LoginUnlockMethod.MasterPassword,
            "Unlock with master password",
            string.Empty,
            true,
            "Master password",
            "Unlock with master password",
            UnlockCommandCommand);
    }

    public LoginPageViewModel ParentViewModel { get; }
    public LoginUnlockMethodItem Method { get; }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private Task UnlockCommandAsync() => TryUnlockAsync();

    internal Task TryUnlockAsync(CancellationToken cancellationToken = default)
    {
        ParentViewModel.ClearStatus();

        if (!Method.IsAvailable)
        {
            ParentViewModel.ShowError("This session cannot be unlocked with the master password.");
            return Task.CompletedTask;
        }

        if (!Method.TryValidateForSubmit())
        {
            return Task.CompletedTask;
        }

        return UnlockCoreAsync(Method.SecretInput, cancellationToken);
    }

    private async Task UnlockCoreAsync(string masterPassword, CancellationToken cancellationToken)
    {
        bool wasInitialized = ParentViewModel.HasInitializedLocalVaultUnlocker;

        try
        {
            await ParentViewModel.RunBusyAsync(async () =>
            {
                await _masterPasswordUnlockService.UnlockAsync(ParentViewModel.RequireSession(), masterPassword, cancellationToken);

                await ParentViewModel.CompleteUnlockAsync(recommendUnlockSetup: !wasInitialized);
            });
        }
        catch (Exception ex)
        {
            ParentViewModel.ShowError(ex);
        }
    }

    public void SetAvailability(bool isAvailable)
        => Method.SetAvailability(isAvailable);

    public void Reset()
        => Method.Reset();
}

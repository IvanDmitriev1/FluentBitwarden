using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Abstractions.UnlockServices;
using FluentBitwarden.Models;

namespace FluentBitwarden.ViewModels;

internal partial class PinUnlockViewModel : ObservableObject
{
    private readonly IPinUnlockService _pinUnlockService;

    public PinUnlockViewModel(
        LoginPageViewModel parentViewModel,
        IPinUnlockService pinUnlockService)
    {
        ParentViewModel = parentViewModel;
        _pinUnlockService = pinUnlockService;
        Method = new LoginUnlockMethodItem(
            LoginUnlockMethod.Pin,
            "Unlock with PIN",
            string.Empty,
            true,
            "App PIN",
            "Unlock with PIN",
            UnlockCommandCommand);
    }

    public LoginPageViewModel ParentViewModel { get; }
    public LoginUnlockMethodItem Method { get; }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private Task UnlockCommandAsync()
        => TryUnlockAsync();

    internal Task TryUnlockAsync(CancellationToken cancellationToken = default)
    {
        ParentViewModel.ClearStatus();

        if (!Method.IsAvailable)
        {
            ParentViewModel.ShowError("PIN unlock is not available.");
            return Task.CompletedTask;
        }

        if (!Method.TryValidateForSubmit())
        {
            return Task.CompletedTask;
        }

        string normalizedPin = Method.SecretInput.Trim();
        return UnlockCoreAsync(normalizedPin, cancellationToken);
    }

    private async Task UnlockCoreAsync(string normalizedPin, CancellationToken cancellationToken)
    {
        try
        {
            await ParentViewModel.RunBusyAsync(async () =>
            {
                await _pinUnlockService.UnlockAsync(ParentViewModel.RequireSession(), normalizedPin, cancellationToken);
                await ParentViewModel.CompleteUnlockAsync();
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

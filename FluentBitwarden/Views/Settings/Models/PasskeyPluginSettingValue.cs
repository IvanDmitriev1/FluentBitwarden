using System.Diagnostics;
using FluentBitwarden.Contracts.AppState;

namespace FluentBitwarden.Views.Settings.Models;

public sealed partial class PasskeyPluginSettingValue : ObservableObject
{
    private bool _isLoading = true;
    private bool _isRollingBackEnabled;

    public PasskeyPluginSettingValue()
    {
        Enabled = AppSettingKeys.Passkeys.PluginEnabledKey.CreateSettingValue(OnEnabledChanged);
    }

    public SettingValue<bool> Enabled { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanToggle))]
    public partial bool IsSupported { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanToggle))]
    public partial bool IsApplying { get; private set; }

    [ObservableProperty]
    public partial string InfoBarStatus { get; private set; } = string.Empty;

    public bool CanToggle => IsSupported && !IsApplying;

    public void Load()
    {
        IsSupported = PasskeyPluginSetupService.IsSupported();
        if (!IsSupported)
        {
            InfoBarStatus = "Passkey plugin integration requires Windows 11 24H2 or newer";
        }

        _isLoading = false;
    }

    private void OnEnabledChanged(bool enabled)
    {
        if (_isLoading || !IsSupported || _isRollingBackEnabled)
            return;

        _ = ApplyEnabledAsync(enabled);
    }

    private async Task ApplyEnabledAsync(bool enabled)
    {
        IsApplying = true;

        try
        {
            Task task = enabled switch
            {
                true => PasskeyPluginSetupService.EnsureRegisteredAsync(),
                false => PasskeyPluginSetupService.UnregisterAsync(),
            };

            await task;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            _isRollingBackEnabled = true;

            try
            {
                Enabled.Value = !enabled;
            }
            finally
            {
                _isRollingBackEnabled = false;
            }
        }
        finally
        {
            IsApplying = false;
        }
    }
}

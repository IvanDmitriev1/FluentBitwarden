using System.Diagnostics;
using FluentBitwarden.Contracts.Infrastructure.Settings.Models;

namespace FluentBitwarden.Views.Settings.Models;

public abstract partial class IntegrationSetupSettingValue : ObservableObject
{
    private bool _isLoading = true;

    protected IntegrationSetupSettingValue(SettingKey<bool> enabledKey)
    {
        Enabled = enabledKey.CreateSettingValue(OnEnabledChanged);
    }

    public SettingValue<bool> Enabled { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanToggle))]
    public partial bool IsApplying { get; private set; }

    public bool CanToggle => CanApply && !IsApplying;

    protected virtual bool CanApply => true;

    public void Load()
    {
        OnLoad();
        _isLoading = false;
        OnPropertyChanged(nameof(CanToggle));
    }

    protected virtual void OnLoad() { }
    protected abstract Task EnableAsync();
    protected abstract Task DisableAsync();

    protected virtual void OnApplySucceeded(bool enabled) { }
    protected virtual void OnApplyFailed(bool enabled, Exception exception) { }

    private void OnEnabledChanged(bool enabled)
    {
        if (_isLoading || IsApplying || !CanApply)
        {
            return;
        }

        _ = ApplyEnabledAsync(enabled);
    }

    private async Task ApplyEnabledAsync(bool enabled)
    {
        IsApplying = true;

        try
        {
            Task task = enabled ? EnableAsync() : DisableAsync();
            await task;
            OnApplySucceeded(enabled);
        }
        catch (Exception ex)
        {
            OnApplyFailed(enabled, ex);
            Enabled.Value = !enabled;
        }
        finally
        {
            IsApplying = false;
        }
    }
}

using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentBitwarden.Contracts.Settings.Models;

namespace FluentBitwarden.Ui.Features.Settings.Models;

public abstract partial class IntegrationSetupSettingValue : ObservableObject
{
    private bool _isLoading = true;

    protected IntegrationSetupSettingValue(SettingKey<bool> enabledKey) =>
        Enabled = enabledKey.CreateSettingValue(OnEnabledChanged);

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
            return;

        ApplyEnabledAsync(enabled).SafeFireAndForget();
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "A failed integration toggle must restore the previous visible setting state.")]
    private async Task ApplyEnabledAsync(bool enabled)
    {
        IsApplying = true;
        try
        {
            if (enabled)
                await EnableAsync();
            else
                await DisableAsync();

            OnApplySucceeded(enabled);
        }
        catch (Exception exception)
        {
            OnApplyFailed(enabled, exception);
            Enabled.Value = !enabled;
        }
        finally
        {
            IsApplying = false;
        }
    }
}

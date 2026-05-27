using FluentBitwarden.Contracts.AppState;
using FluentBitwarden.Contracts.AppState.Models;

namespace FluentBitwarden.Views.Settings.Models;

public sealed partial class SettingValue<T> : ObservableObject where T : notnull
{
    public SettingValue(SettingKey<T> key, Action<T>? afterChanged = null)
    {
        _key = key;
        _afterChanged = afterChanged;

        _suppressChange = true;
        Value = SettingsStore.Instance.Get(_key);
        _suppressChange = false;
    }

    private readonly SettingKey<T> _key;
    private readonly Action<T>? _afterChanged;
    private bool _suppressChange;


    [ObservableProperty]
    public partial T Value { get; set; }

    public void Reload()
    {
        _suppressChange = true;
        try
        {
            Value = SettingsStore.Instance.Get(_key);
        }
        finally
        {
            _suppressChange = false;
        }
    }

    public void Reset()
    {
        Value = _key.DefaultValue;
    }

    partial void OnValueChanged(T value)
    {
        if (_suppressChange)
            return;

        SettingsStore.Instance.Set(_key, value);
        _afterChanged?.Invoke(value);
    }
}

public static class SettingValueExtension
{
    public static SettingValue<T> CreateSettingValue<T>(this SettingKey<T> key, Action<T>? afterChanged = null) where T : notnull =>
        new(key, afterChanged);
}

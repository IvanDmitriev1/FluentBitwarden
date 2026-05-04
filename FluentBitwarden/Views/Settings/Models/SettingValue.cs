using FluentBitwarden.Modules.AppState;
using FluentBitwarden.Modules.AppState.Models;

namespace FluentBitwarden.Views.Settings.Models;

public sealed partial class SettingValue<T> : ObservableObject where T : notnull
{
    public SettingValue(SettingKey<T> key, Action<T>? afterChanged = null)
    {
        _key = key;
        _afterChanged = afterChanged;

        Value = SettingsStore.Instance.Get(_key);
    }

    private readonly SettingKey<T> _key;
    private readonly Action<T>? _afterChanged;


    [ObservableProperty]
    public partial T Value { get; set; }

    partial void OnValueChanged(T value)
    {
        SettingsStore.Instance.Set(_key, value);
        _afterChanged?.Invoke(value);
    }
}

public static class SettingValueExtension
{
    public static SettingValue<T> Create<T>(this SettingKey<T> key, Action<T>? afterChanged = null) where T : notnull =>
        new(key, afterChanged);
}
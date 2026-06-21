using FluentBitwarden.Contracts.Settings.Models;

namespace FluentBitwarden.Contracts.Settings;

public interface ISettingsStore
{
    event EventHandler<SettingChangedEventArgs> Changed;

    void Clear();

    T Get<T>(SettingKey<T> key) where T : notnull;
    void Set<T>(SettingKey<T> key, T value) where T : notnull;
    bool Remove<T>(SettingKey<T> key) where T : notnull;
}

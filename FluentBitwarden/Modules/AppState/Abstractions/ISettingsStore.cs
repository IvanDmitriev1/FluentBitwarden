using FluentBitwarden.Modules.AppState.Models;

namespace FluentBitwarden.Modules.AppState.Abstractions;

public interface ISettingsStore
{
    event EventHandler<SettingChangedEventArgs> Changed;
    
    void Clear();

    T Get<T>(SettingKey<T> key) where T : notnull;
    void Set<T>(SettingKey<T> key, T value) where T : notnull;
    bool Remove<T>(SettingKey<T> key) where T : notnull;

    T GetComposite<T>(CompositeSettingKey<T> key) where T : notnull;
    void SetComposite<T>(CompositeSettingKey<T> key, T value) where T : notnull;
    bool RemoveComposite<T>(CompositeSettingKey<T> key) where T : notnull;
}
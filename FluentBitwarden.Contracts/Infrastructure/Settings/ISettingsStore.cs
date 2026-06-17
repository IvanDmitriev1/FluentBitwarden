using FluentBitwarden.Contracts.Infrastructure.Settings.Models;

namespace FluentBitwarden.Contracts.Infrastructure.Settings;

public interface ISettingsStore
{
    event EventHandler<SettingChangedEventArgs> Changed;

    void Clear();

    T Get<T>(SettingKey<T> key) where T : notnull;
    void Set<T>(SettingKey<T> key, T value) where T : notnull;
    bool Remove<T>(SettingKey<T> key) where T : notnull;

    T GetComposite<T>(CompositeSettingKey<T> key) where T : ICompositeSettingValue<T>;
    void SetComposite<T>(CompositeSettingKey<T> key, T value) where T : ICompositeSettingValue<T>;
    bool RemoveComposite<T>(CompositeSettingKey<T> key) where T : ICompositeSettingValue<T>;
}
using FluentBitwarden.Contracts.Settings;

namespace FluentBitwarden.Platform.Settings.Composition;

public interface ICompositeSettingsStore : ISettingsStore
{
    T GetComposite<T>(CompositeSettingKey<T> key) where T : ICompositeSettingValue<T>;
    void SetComposite<T>(CompositeSettingKey<T> key, T value) where T : ICompositeSettingValue<T>;
    bool RemoveComposite<T>(CompositeSettingKey<T> key) where T : ICompositeSettingValue<T>;
}

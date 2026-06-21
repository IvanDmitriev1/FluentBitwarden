using FluentBitwarden.Contracts.Settings;
using FluentBitwarden.Platform.Settings.Models;

namespace FluentBitwarden.Platform.Settings;

public interface ICompositeSettingsStore : ISettingsStore
{
    T GetComposite<T>(CompositeSettingKey<T> key) where T : ICompositeSettingValue<T>;
    void SetComposite<T>(CompositeSettingKey<T> key, T value) where T : ICompositeSettingValue<T>;
    bool RemoveComposite<T>(CompositeSettingKey<T> key) where T : ICompositeSettingValue<T>;
}

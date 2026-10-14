using Windows.Storage;

namespace FluentBitwarden.Platform.Settings.Composition;

public interface ICompositeSettingValue<TThis> where TThis : ICompositeSettingValue<TThis>
{
    static abstract void Write(ApplicationDataCompositeValue composite, TThis value);
    static abstract bool TryRead(ApplicationDataCompositeValue composite, [NotNullWhen(true)] out TThis? value);
}

public sealed record CompositeSettingKey<T>(string Name, T DefaultValue) where T : ICompositeSettingValue<T>;
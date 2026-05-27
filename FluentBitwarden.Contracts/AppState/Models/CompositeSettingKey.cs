using Windows.Storage;

namespace FluentBitwarden.Contracts.AppState.Models;

public sealed record CompositeSettingKey<T>(
    string Name,
    T DefaultValue,
    CompositeSettingKey<T>.TryReadComposite TryRead,
    CompositeSettingKey<T>.WriteComposite Write) where T : notnull
{
    public delegate bool TryReadComposite(
        ApplicationDataCompositeValue composite,
        out T value);

    public delegate void WriteComposite(
        ApplicationDataCompositeValue composite,
        T value);
}
using System.Globalization;
using System.Text.Json;
using Windows.Foundation;
using Windows.Storage;
using FluentBitwarden.Contracts.Modules.AppState.Abstractions;
using FluentBitwarden.Contracts.Modules.AppState.Models;

namespace FluentBitwarden.Contracts.Modules.AppState.Services;

internal sealed class ApplicationDataSettingsStore : ISettingsStore
{
    private readonly Lock _lock = new();
    private readonly ApplicationDataContainer _container = ApplicationData.Current.LocalSettings;

    public event EventHandler<SettingChangedEventArgs>? Changed;

    public T Get<T>(SettingKey<T> key) where T : notnull
    {
        if (!_container.Values.TryGetValue(key.Name, out var rawValue) || rawValue is null)
            return key.DefaultValue;

        try
        {
            return !TryConvertFromStorageValue<T>(rawValue, out var value)
                ? key.DefaultValue
                : value;
        }
        catch (JsonException)
        {
            return key.DefaultValue;
        }
        catch (InvalidCastException)
        {
            return key.DefaultValue;
        }
    }

    public void Set<T>(SettingKey<T> key, T value) where T : notnull
    {
        using var _ = _lock.EnterScope();

        _container.Values[key.Name] = ConvertToStorageValue(value);
        OnChanged(key.Name);
    }

    public bool Remove<T>(SettingKey<T> key) where T : notnull
    {
        using var _ = _lock.EnterScope();

        bool removed = _container.Values.Remove(key.Name);
        if (removed)
        {
            OnChanged(key.Name);
        }

        return removed;
    }

    public T GetComposite<T>(CompositeSettingKey<T> key) where T : notnull
    {
        if (!_container.Values.TryGetValue(key.Name, out var raw) || 
            raw is not ApplicationDataCompositeValue composite)
        {
            return key.DefaultValue;
        }

        return key.TryRead.Invoke(composite, out var value)
            ? value
            : key.DefaultValue;
    }

    public void SetComposite<T>(CompositeSettingKey<T> key, T value) where T : notnull
    {
        using var _ = _lock.EnterScope();

        var composite = new ApplicationDataCompositeValue();
        key.Write.Invoke(composite, value);
        _container.Values[key.Name] = composite;

        OnChanged(key.Name);
    }

    public bool RemoveComposite<T>(CompositeSettingKey<T> key) where T : notnull
    {
        using var _ = _lock.EnterScope();

        bool removed = _container.Values.Remove(key.Name);
        if (removed)
        {
            OnChanged(key.Name);
        }

        return removed;
    }

    public void Clear()
    {
        using var _ = _lock.EnterScope();
    }

    private void OnChanged(string name)
    {
        Changed?.Invoke(this, new SettingChangedEventArgs(name));
    }

    private static object ConvertToStorageValue<T>(T value) where T : notnull
    {
        Type type = typeof(T);

        if (type.IsEnum)
        {
            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        if (IsSupportedStorageValue(value))
        {
            return value;
        }

        throw new NotSupportedException(
            $"Setting type '{type.FullName}' is not supported by " +
            $"{nameof(ApplicationDataSettingsStore)}. " +
            "Use a supported ApplicationData setting type, a composite setting, " +
            "or a separate file-based store.");
    }

    private static bool TryConvertFromStorageValue<T>(object rawValue, [MaybeNullWhen(false)] out T value) where T : notnull
    {
        Type targetType = typeof(T);

        if (rawValue is T typedValue)
        {
            value = typedValue;
            return true;
        }

        if (targetType.IsEnum && rawValue is int enumValue)
        {
            value = (T)Enum.ToObject(targetType, enumValue);
            return true;
        }

        value = default;
        return false;
    }

    private static bool IsSupportedStorageValue(object value)
    {
        return value is byte
            or short
            or ushort
            or int
            or uint
            or long
            or ulong
            or float
            or double
            or bool
            or char
            or string
            or DateTimeOffset
            or TimeSpan
            or Guid
            or Point
            or Size
            or Rect;
    }
}
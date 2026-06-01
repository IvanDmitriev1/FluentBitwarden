using Microsoft.UI.Xaml.Data;

namespace FluentBitwarden.Shared.Converters;

public interface IOptionItem
{
    string Title { get; }
}

public interface IOptionItem<out T> : IOptionItem
{
    T Value { get; }
}

public abstract class OptionItemConverter<TValue, TOption>(IReadOnlyList<TOption> options) : IValueConverter
    where TOption : IOptionItem<TValue>
{
    private IReadOnlyList<TOption> Options { get; } = options;

    public object? Convert(object? value, Type targetType, object parameter, string language)
    {
        if (value is not TValue typedValue)
            return Options[0];

        foreach (var option in Options)
        {
            if (EqualityComparer<TValue>.Default.Equals(option.Value, typedValue))
                return option;
        }

        return Options[0];
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is TOption option
            ? option.Value!
            : Options[0].Value!;
    }
}

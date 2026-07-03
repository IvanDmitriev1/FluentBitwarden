using System.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace FluentBitwarden.Infrastructure.Converters;

internal sealed partial class EmptyCollectionToObjectConverter : IValueConverter
{
    public Visibility EmptyValue { get; set; }
    public Visibility NotEmptyValue { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is null)
            return EmptyValue;

        if (value is ICollection collection)
            return collection.Count == 0 ? EmptyValue : NotEmptyValue;

        return EmptyValue;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}

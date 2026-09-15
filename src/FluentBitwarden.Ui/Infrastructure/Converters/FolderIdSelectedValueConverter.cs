using Microsoft.UI.Xaml.Data;

namespace FluentBitwarden.Infrastructure.Converters;

internal sealed partial class FolderIdSelectedValueConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, string language) => value;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        value is FolderId folderId ? folderId : FolderId.Empty;
}

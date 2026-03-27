using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace FluentBitwarden.Resources.Controls;

public sealed class ValidatableProperty
{
    private ValidatableProperty(INotifyDataErrorInfo source, string propertyName)
    {
        Source = source;
        PropertyName = propertyName;
    }

    public INotifyDataErrorInfo Source { get; }
    public string PropertyName { get; }

    public static ValidatableProperty Create<TSource, TProperty>(
        TSource source,
        Expression<Func<TSource, TProperty>> propertyExpression)
        where TSource : class, INotifyDataErrorInfo
    {
        if (propertyExpression.Body is not MemberExpression
            {
                Expression: ParameterExpression, Member: PropertyInfo propertyInfo
            })
        {
            throw new ArgumentException(
                "Validation target must be a direct property expression (x => x.Property).",
                nameof(propertyExpression));
        }

        return new ValidatableProperty(source, propertyInfo.Name);
    }
}
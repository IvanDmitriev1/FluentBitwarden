using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace FluentBitwarden.Shared.Validation;

public sealed class ValidatableProperty
{
    private ValidatableProperty(
        INotifyDataErrorInfo source,
        string propertyName)
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
        var propertyInfo = GetDirectPropertyInfo(propertyExpression);
        return new ValidatableProperty(source, propertyInfo.Name);
    }
    private static PropertyInfo GetDirectPropertyInfo<TSource, TProperty>(
        Expression<Func<TSource, TProperty>> propertyExpression)
    {
        Expression body = propertyExpression.Body;

        if (body is UnaryExpression
            {
                NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked
            } unaryExpression)
        {
            body = unaryExpression.Operand;
        }

        if (body is not MemberExpression
            {
                Expression: ParameterExpression,
                Member: PropertyInfo propertyInfo
            })
        {
            throw new ArgumentException(
                "Validation target must be a direct property expression, for example: x => x.Property.",
                nameof(propertyExpression));
        }

        return propertyInfo;
    }
}
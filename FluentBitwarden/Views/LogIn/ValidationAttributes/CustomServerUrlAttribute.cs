using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace FluentBitwarden.Views.LogIn.ValidationAttributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class CustomServerUrlAttribute<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TSelf>(
    string shouldValidateMemberName) : ValidationAttribute
{
    protected override ValidationResult? IsValid(
        object? value,
        ValidationContext validationContext)
    {
        if (validationContext.ObjectInstance is not TSelf self)
            return new ValidationResult($"Validation object must be of type {typeof(TSelf).Name}.");

        if (!GetBooleanPropertyValue(self, shouldValidateMemberName))
            return ValidationResult.Success;

        var text = value as string;
        if (string.IsNullOrWhiteSpace(text))
            return new ValidationResult("Enter your self-hosted server URL.");

        if (!Uri.TryCreate(text.Trim(), UriKind.Absolute, out var uri))
        {
            return new ValidationResult(
                "Enter a valid server URL.");
        }

        if (uri.Scheme is not "https")
        {
            return new ValidationResult(
                "The server URL must use HTTPS.");
        }

        return ValidationResult.Success;
    }

    private static bool GetBooleanPropertyValue(TSelf self, string propName)
    {
        var property = typeof(TSelf).GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
        if (property is null)
            throw new InvalidOperationException($"Property '{propName}' was not found on type '{typeof(TSelf).Name}'.");

        if (property.PropertyType != typeof(bool))
            throw new InvalidOperationException($"Property '{propName}' must be a bool property.");

        return (bool)(property.GetValue(self) ?? false);
    }
}
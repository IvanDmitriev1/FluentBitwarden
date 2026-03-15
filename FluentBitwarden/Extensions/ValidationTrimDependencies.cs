using System.Diagnostics.CodeAnalysis;

namespace FluentBitwarden.Extensions;

internal static class ValidationTrimDependencies
{
    [DynamicDependency(
        DynamicallyAccessedMemberTypes.PublicMethods,
        "CommunityToolkit.Mvvm.ComponentModel.__Internals.__ObservableValidatorExtensions",
        "FluentBitwarden")]
    internal static void Preserve()
    {
    }
}

using System.Diagnostics.CodeAnalysis;

namespace FluentBitwarden.Infrastructure;

internal static class TrimmingConfiguration
{
    [DynamicDependency(
        DynamicallyAccessedMemberTypes.PublicMethods,
        "CommunityToolkit.Mvvm.ComponentModel.__Internals.__ObservableValidatorExtensions",
        "FluentBitwarden")]
    internal static void Preserve()
    {
    }
}

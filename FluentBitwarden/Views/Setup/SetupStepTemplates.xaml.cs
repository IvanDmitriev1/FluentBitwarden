using Microsoft.UI.Xaml;
using System.Diagnostics.CodeAnalysis;

namespace FluentBitwarden.Views.Setup;

[UnconditionalSuppressMessage(
    "Trimming",
    "IL2026",
    Justification = "Generated XAML bindings call validation-backed setters on trim-aware ObservableValidator viewmodels.")]
public sealed partial class SetupStepTemplates : ResourceDictionary
{
    public SetupStepTemplates()
    {
        InitializeComponent();
    }
}

namespace FluentBitwarden.Shared.Services.Abstractions.Dialog;

public sealed record ContentDialogOptions(
    string Title,
    string PrimaryButtonText,
    string SecondaryButtonText,
    ContentDialogButton DefaultButton,
    string DataTemplateKey);
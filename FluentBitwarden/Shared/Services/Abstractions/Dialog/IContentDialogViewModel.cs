namespace FluentBitwarden.Shared.Services.Abstractions.Dialog;

public interface IContentDialogViewModel
{
    string DataTemplateKey { get; }
    string DialogTitle { get; }
}
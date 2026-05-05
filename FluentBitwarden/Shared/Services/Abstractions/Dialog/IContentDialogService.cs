using FluentBitwarden.Resources.Dialogs.Models;

namespace FluentBitwarden.Shared.Services.Abstractions.Dialog;

internal interface IContentDialogService
{
    Task<UserActionDialogOutcome> ShowUserActionAsync(object viewModel, ContentDialogOptions options);
}
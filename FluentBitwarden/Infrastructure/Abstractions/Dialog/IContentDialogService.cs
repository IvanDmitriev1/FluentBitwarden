using FluentBitwarden.Resources.Dialogs.Models;

namespace FluentBitwarden.Infrastructure.Abstractions.Dialog;

internal interface IContentDialogService
{
    Task<UserActionDialogOutcome> ShowUserActionAsync(object viewModel, ContentDialogOptions options);
}
using FluentBitwarden.Contracts.Infrastructure.Shared;
using FluentBitwarden.Resources.Dialogs;

namespace FluentBitwarden.Infrastructure.Abstractions.Dialog;

internal interface IContentDialogService
{
    Task<UserActionDialogOutcome> ShowUserActionAsync(object viewModel, ContentDialogOptions options);
}
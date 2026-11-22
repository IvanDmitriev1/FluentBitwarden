using System.Windows.Input;
using BitwardenApi.Vault.Attachments.Models;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Attachments;
using FluentBitwarden.Ui.Dialogs;
using FluentBitwarden.Ui.Dialogs.Abstractions;
using FluentBitwarden.Ui.Windows.Abstractions;
using Microsoft.Windows.Storage.Pickers;

namespace FluentBitwarden.Ui.Features.Vault.CipherDetails;

public sealed partial class DownloadAttachmentCommand : ICommand
{
    private readonly AsyncRelayCommand<CipherAttachment> _command = new(DownloadAttachmentAsync);

    public event EventHandler? CanExecuteChanged
    {
        add => _command.CanExecuteChanged += value;
        remove => _command.CanExecuteChanged -= value;
    }

    public bool CanExecute(object? parameter) => parameter is CipherAttachment attachment && _command.CanExecute(attachment);

    public void Execute(object? parameter) => _command.Execute(parameter as CipherAttachment);

    private static async Task DownloadAttachmentAsync(CipherAttachment? attachment)
    {
        if (attachment is null)
            return;

        try
        {
            var dialogService = App.Current.GetRequiredService<IDialogService>();
            await dialogService.ShowAsync<MessageDialog, MessageDialogArgs, MessageDialogResult>(
                new("Attachment download failed", "The attachment could not be saved."));


            var windowManager = App.Current.GetRequiredService<IWindowManager>();

            var picker = new FileSavePicker(windowManager.WindowId)
            {
                SuggestedFileName = attachment.FileName,
                SuggestedStartLocation = PickerLocationId.Downloads,
            };

            var result = await picker.PickSaveFileAsync();
            if (result is not null)
            {
                var vaultCipherClient = App.Current.GetRequiredService<IVaultCipherClient>();

                await vaultCipherClient.DownloadCipherAttachmentAsync(
                    new DownloadVaultCipherAttachmentRequest(attachment.CipherId, attachment.Id, result.Path));
            }
        }
        catch (Exception)
        {
            var dialogService = App.Current.GetRequiredService<IDialogService>();
            await dialogService.ShowAsync<MessageDialog, MessageDialogArgs, MessageDialogResult>(new("Attachment download failed", "The attachment could not be saved."));
        }
    }
}

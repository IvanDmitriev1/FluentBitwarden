using BitwardenApi.Vault.Attachments.Contracts;
using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Contracts.Modules.Vault.Workspace;
using Humanizer;
using Microsoft.UI.Xaml;
using Microsoft.Windows.Storage.Pickers;

namespace FluentBitwarden.Controls.VaultCiphers;

[TemplatePart(Name = PartChrome, Type = typeof(VaultCipherFieldChrome))]
[TemplatePart(Name = PartFileNameTextBlock, Type = typeof(TextBlock))]
[TemplatePart(Name = PartSizeTextBlock, Type = typeof(TextBlock))]
[DependencyProperty<VaultCipherAttachment>("Attachment")]
public sealed partial class VaultCipherAttachmentField : Control
{
    private const string PartChrome = "PART_Chrome";
    private const string PartFileNameTextBlock = "PART_FileNameTextBlock";
    private const string PartSizeTextBlock = "PART_SizeTextBlock";

    private VaultCipherFieldChrome? _chrome;
    private TextBlock? _fileNameTextBlock;
    private TextBlock? _sizeTextBlock;

    public VaultCipherAttachmentField()
    {
        DefaultStyleKey = typeof(VaultCipherAttachmentField);
    }

    protected override void OnApplyTemplate()
    {
        _chrome?.Click -= OnChromeClick;

        base.OnApplyTemplate();

        _chrome = GetTemplateChild(PartChrome) as VaultCipherFieldChrome;
        _fileNameTextBlock = GetTemplateChild(PartFileNameTextBlock) as TextBlock;
        _sizeTextBlock = GetTemplateChild(PartSizeTextBlock) as TextBlock;

        _chrome?.Click += OnChromeClick;

        OnAttachmentChanged();
    }

    partial void OnAttachmentChanged()
    {
        if (Attachment is null || _fileNameTextBlock is null || _sizeTextBlock is null)
            return;

        _fileNameTextBlock.Text = Attachment.FileName;
        _sizeTextBlock.Text = Attachment.Size.Bytes.Bytes().Humanize();
    }

    private async void OnChromeClick(SplitButton sender, SplitButtonClickEventArgs args)
    {
        if (Attachment is null)
            return;

        ArgumentNullException.ThrowIfNull(_chrome);

        try
        {
            _chrome.IsEnabled = false;

            var fileSavePicker = new FileSavePicker(XamlRoot.ContentIslandEnvironment.AppWindowId)
            {
                SuggestedFileName = Attachment.FileName,
                SuggestedStartLocation = PickerLocationId.Downloads
            };

            var result = await fileSavePicker.PickSaveFileAsync();
            if (result is null)
                return;

            await App.Current.GetRequiredService<IVaultClient>()
                .DownloadCipherAttachmentAsync(new DownloadVaultCipherAttachmentRequest(Attachment, result.Path));
        }
        finally
        {
            _chrome.IsEnabled = true;
        }
    }
}

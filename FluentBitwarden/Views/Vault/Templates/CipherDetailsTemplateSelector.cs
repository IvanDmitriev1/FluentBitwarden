namespace FluentBitwarden.Views.Vault.Templates;

using BitwardenApi.Modules.Vault.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

public sealed class CipherDetailsTemplateSelector : DataTemplateSelector
{
    public DataTemplate? LoginTemplate { get; set; }
    public DataTemplate? SecureNoteTemplate { get; set; }
    public DataTemplate? CardTemplate { get; set; }
    public DataTemplate? IdentityTemplate { get; set; }
    public DataTemplate? SshKeyTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item)
    {
        return item switch
        {
            LoginCipher => LoginTemplate,
            SecureNoteCipher => SecureNoteTemplate,
            CardCipher => CardTemplate,
            IdentityCipher => IdentityTemplate,
            SshKeyCipher => SshKeyTemplate,
            _ => base.SelectTemplateCore(item)
        };
    }

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        return SelectTemplateCore(item);
    }
}
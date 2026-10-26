using Microsoft.UI.Xaml;

namespace FluentBitwarden.Templates.Vault;

public sealed partial class VaultCipherTemplateSelector : DataTemplateSelector
{
    public DataTemplate? LoginTemplate { get; set; }
    public DataTemplate? SecureNoteTemplate { get; set; }
    public DataTemplate? CardTemplate { get; set; }
    public DataTemplate? IdentityTemplate { get; set; }
    public DataTemplate? SshKeyTemplate { get; set; }
    public DataTemplate? FallbackTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item) =>
        item switch
        {
            LoginCipher => LoginTemplate,
            SecureNoteCipher => SecureNoteTemplate,
            CardCipher => CardTemplate,
            IdentityCipher => IdentityTemplate,
            SshKeyCipher => SshKeyTemplate,
            _ => FallbackTemplate ?? base.SelectTemplateCore(item),
        };

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container) =>
        SelectTemplateCore(item);
}

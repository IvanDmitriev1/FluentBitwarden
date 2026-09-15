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
            LoginVaultCipher => LoginTemplate,
            SecureNoteVaultCipher => SecureNoteTemplate,
            CardVaultCipher => CardTemplate,
            IdentityVaultCipher => IdentityTemplate,
            SshKeyVaultCipher => SshKeyTemplate,
            _ => FallbackTemplate ?? base.SelectTemplateCore(item),
        };

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container) =>
        SelectTemplateCore(item);
}

using MemoryPack;




using BitwardenApi.Vault.Attachments.Models;
using BitwardenApi.Serialization;

namespace BitwardenApi.Vault.Ciphers.Models;
/// <summary>
/// Common vault vaultCipher fields.
/// <see cref="CardCipher"/>, <see cref="IdentityCipher"/>, <see cref="SecureNoteCipher"/>.
/// </summary>
[MemoryPackable]
[MemoryPackUnion(0, typeof(LoginCipher))]
[MemoryPackUnion(1, typeof(SecureNoteCipher))]
[MemoryPackUnion(2, typeof(CardCipher))]
[MemoryPackUnion(3, typeof(IdentityCipher))]
[MemoryPackUnion(4, typeof(SshKeyCipher))]
public abstract partial class Cipher
{
    [MemoryPackIgnore]
    public abstract CipherType Type { get; }

    [StronglyTypedIdFormatter<CipherId>]
    public required CipherId Id { get; set; }

    [StronglyTypedIdFormatter<FolderId>]
    public FolderId FolderId { get; set; }

    public required string Name { get; set; }
    public string? Notes { get; set; }
    public required bool Favorite { get; set; }
    public required bool Reprompt { get; set; }
    public required DateTimeOffset RevisionDate { get; set; }
    public required DateTimeOffset CreationDate { get; set; }
    public required DateTimeOffset? DeletedDate { get; set; }

    public CipherAttachment[] Attachments { get; set; } = [];

    /// <summary>
    /// Creates an empty, unsaved cipher of the requested <paramref name="type"/>,
    /// ready to be populated in the editor.
    /// </summary>
    public static Cipher CreateBlankCipher(CipherType type)
    {
        var timestamp = DateTimeOffset.UtcNow;

        return type switch
        {
            CipherType.Login => new LoginCipher { Id = CipherId.Empty, Name = string.Empty, Favorite = false, Reprompt = false, RevisionDate = timestamp, CreationDate = timestamp, DeletedDate = null },
            CipherType.SecureNote => new SecureNoteCipher { Id = CipherId.Empty, Name = string.Empty, Favorite = false, Reprompt = false, RevisionDate = timestamp, CreationDate = timestamp, DeletedDate = null },
            CipherType.Card => new CardCipher { Id = CipherId.Empty, Name = string.Empty, Favorite = false, Reprompt = false, RevisionDate = timestamp, CreationDate = timestamp, DeletedDate = null },
            CipherType.Identity => new IdentityCipher { Id = CipherId.Empty, Name = string.Empty, Favorite = false, Reprompt = false, RevisionDate = timestamp, CreationDate = timestamp, DeletedDate = null },
            CipherType.SshKey => new SshKeyCipher { Id = CipherId.Empty, Name = string.Empty, Favorite = false, Reprompt = false, RevisionDate = timestamp, CreationDate = timestamp, DeletedDate = null },
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown vault cipher type."),
        };
    }
}

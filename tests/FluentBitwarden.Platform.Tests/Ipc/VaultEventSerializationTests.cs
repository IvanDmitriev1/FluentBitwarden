using BitwardenApi.Primitives.Ids;
using BitwardenApi.Vault.Ciphers.Models;
using BitwardenApi.Vault.Folders.Models;
using FluentBitwarden.Contracts.Modules.Vault.Events;
using MemoryPack;

namespace FluentBitwarden.Platform.Tests.Ipc;

public sealed class VaultEventSerializationTests
{
    private static readonly UserId UserId = UserId.Parse("11111111-1111-1111-1111-111111111111");

    [Theory]
    [InlineData(CipherType.Login)]
    [InlineData(CipherType.SecureNote)]
    [InlineData(CipherType.Card)]
    [InlineData(CipherType.Identity)]
    [InlineData(CipherType.SshKey)]
    public void Changed_cipher_preserves_polymorphic_state(CipherType type)
    {
        Cipher cipher = Cipher.CreateBlankCipher(type);
        cipher.Id = CipherId.Parse("22222222-2222-2222-2222-222222222222");
        cipher.Name = "Updated item";
        cipher.Notes = "Updated notes";

        CipherChangedEvent restored = RoundTrip(new CipherChangedEvent(UserId, cipher));

        Assert.Equal(UserId, restored.UserId);
        Assert.Equal(type, restored.Cipher.Type);
        Assert.Equal(MemoryPackSerializer.Serialize<Cipher>(cipher), MemoryPackSerializer.Serialize<Cipher>(restored.Cipher));
    }

    [Fact]
    public void Deleted_cipher_preserves_account_and_item_identity()
    {
        var message = new CipherDeletedEvent(UserId, CipherId.Parse("22222222-2222-2222-2222-222222222222"));

        Assert.Equal(message, RoundTrip(message));
    }

    [Fact]
    public void Changed_folder_preserves_identity_name_and_revision()
    {
        var folder = new Folder
        {
            Id = FolderId.Parse("33333333-3333-3333-3333-333333333333"),
            Name = "Updated folder",
            RevisionDate = DateTimeOffset.UtcNow
        };

        FolderChangedEvent restored = RoundTrip(new FolderChangedEvent(UserId, folder));

        Assert.Equal(UserId, restored.UserId);
        Folder actual = restored.Folder;
        Assert.Equal(folder.Id, actual.Id);
        Assert.Equal(folder.Name, actual.Name);
        Assert.Equal(folder.RevisionDate, actual.RevisionDate);
    }

    private static T RoundTrip<T>(T message) =>
        MemoryPackSerializer.Deserialize<T>(MemoryPackSerializer.Serialize(message))!;
}

namespace BitwardenApi.Modules.Vault.Models;

public struct FolderDto
{
    public FolderId Id { get; set; }
    public DateTimeOffset RevisionDate { get; set; }
    public string? EncryptedName { get; set; }
}

public struct CollectionDto
{
    public CollectionId Id { get; set; }
    public OrganizationId? OrganizationId { get; set; }
    public bool ReadOnly { get; set; }
    public bool Manage { get; set; }
    public bool HidePasswords { get; set; }
    public int? Type { get; set; }
    public string? EncryptedName { get; set; }
}

public struct CipherDto
{
    public CipherId Id { get; set; }
    public OrganizationId? OrganizationId { get; set; }
    public FolderId? FolderId { get; set; }
    public string? EncryptedKey { get; set; }
    public CipherType CipherType { get; set; }
    public DateTimeOffset RevisionDate { get; set; }
    public DateTimeOffset CreationDate { get; set; }
    public DateTimeOffset? DeletedDate { get; set; }
    public DateTimeOffset? ArchivedDate { get; set; }
    public bool Favorite { get; set; }
    public bool Reprompt { get; set; }
    public bool Edit { get; set; }
    public bool ViewPassword { get; set; }
}

namespace BitwardenApi.Modules.Vault.Models;

public readonly ref struct FolderDto
{
    public required FolderId Id { get; init; }
    public required ReadOnlySpan<byte> Payload { get;init; }
}

public readonly ref struct CollectionDto
{
    public required CollectionId Id { get;init; }
    public required ReadOnlySpan<byte> Payload { get; init; }
}

public readonly ref struct CipherDto
{
    public required CipherId Id { get; init; }
    public required FolderId? FolderId { get; init; }
    public required CipherType CipherType { get; init; }

    public required ReadOnlySpan<byte> Payload { get; init; }
}
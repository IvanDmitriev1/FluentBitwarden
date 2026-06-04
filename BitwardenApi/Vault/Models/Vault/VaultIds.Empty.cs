namespace BitwardenApi.Models;

public readonly partial struct CipherId
{
    public bool IsEmpty => string.IsNullOrEmpty(Value);
}

public readonly partial struct FolderId
{
    public bool IsEmpty => string.IsNullOrEmpty(Value);
}

public readonly partial struct CollectionId
{
    public bool IsEmpty => string.IsNullOrEmpty(Value);
}

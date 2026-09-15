using System.Runtime.CompilerServices;

namespace BitwardenApi.Primitives.Ids;

public static class StronglyTypedIdThrowExtensions
{
    public static void ThrowIfEmpty(
        this UserId id,
        [CallerArgumentExpression(nameof(id))] string? paramName = null)
    {
        if (id.Value == Guid.Empty)
            throw CreateEmptyIdException(nameof(UserId), paramName);
    }

    public static void ThrowIfEmpty(
        this OrganizationId id,
        [CallerArgumentExpression(nameof(id))] string? paramName = null)
    {
        if (id.IsEmpty)
            throw CreateEmptyIdException(nameof(OrganizationId), paramName);
    }

    public static void ThrowIfEmpty(
        this CipherId id,
        [CallerArgumentExpression(nameof(id))] string? paramName = null)
    {
        if (id.IsEmpty)
            throw CreateEmptyIdException(nameof(CipherId), paramName);
    }

    public static void ThrowIfEmpty(
        this FolderId id,
        [CallerArgumentExpression(nameof(id))] string? paramName = null)
    {
        if (id.IsEmpty)
            throw CreateEmptyIdException(nameof(FolderId), paramName);
    }

    public static void ThrowIfEmpty(
        this CollectionId id,
        [CallerArgumentExpression(nameof(id))] string? paramName = null)
    {
        if (id.IsEmpty)
            throw CreateEmptyIdException(nameof(CollectionId), paramName);
    }

    public static void ThrowIfEmpty(
        this AttachmentId id,
        [CallerArgumentExpression(nameof(id))] string? paramName = null)
    {
        if (id.IsEmpty)
            throw CreateEmptyIdException(nameof(AttachmentId), paramName);
    }

    private static ArgumentException CreateEmptyIdException(string typeName, string? paramName) =>
        new($"{typeName} must not be empty.", paramName);
}
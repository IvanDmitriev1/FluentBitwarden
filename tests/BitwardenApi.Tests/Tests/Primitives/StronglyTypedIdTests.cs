namespace BitwardenApi.Tests.Tests.Primitives;

public sealed class StronglyTypedIdTests
{
    [Fact]
    public void ThrowIfEmpty_rejects_each_id_type_with_the_call_site_parameter_name()
    {
        UserId userId = default;
        OrganizationId organizationId = default;
        CipherId cipherId = default;
        FolderId folderId = default;
        CollectionId collectionId = default;
        AttachmentId attachmentId = default;

        AssertEmptyIdThrows(() => userId.ThrowIfEmpty(), nameof(userId), nameof(UserId));
        AssertEmptyIdThrows(() => organizationId.ThrowIfEmpty(), nameof(organizationId), nameof(OrganizationId));
        AssertEmptyIdThrows(() => cipherId.ThrowIfEmpty(), nameof(cipherId), nameof(CipherId));
        AssertEmptyIdThrows(() => folderId.ThrowIfEmpty(), nameof(folderId), nameof(FolderId));
        AssertEmptyIdThrows(() => collectionId.ThrowIfEmpty(), nameof(collectionId), nameof(CollectionId));
        AssertEmptyIdThrows(() => attachmentId.ThrowIfEmpty(), nameof(attachmentId), nameof(AttachmentId));
    }

    [Fact]
    public void ThrowIfEmpty_accepts_nonempty_ids()
    {
        UserId userId = UserId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        OrganizationId organizationId = OrganizationId.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        CipherId cipherId = CipherId.Parse("cipher");
        FolderId folderId = FolderId.Parse("folder");
        CollectionId collectionId = CollectionId.Parse("collection");
        AttachmentId attachmentId = AttachmentId.Parse("attachment");

        userId.ThrowIfEmpty();
        organizationId.ThrowIfEmpty();
        cipherId.ThrowIfEmpty();
        folderId.ThrowIfEmpty();
        collectionId.ThrowIfEmpty();
        attachmentId.ThrowIfEmpty();
    }

    [Fact]
    public void Guid_ids_parse_and_format_their_wire_value()
    {
        const string wireValue = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

        Assert.Equal(Guid.Parse(wireValue), UserId.Parse(wireValue).Value);
        Assert.Equal(Guid.Parse(wireValue), OrganizationId.Parse(wireValue).Value);
    }

    private static void AssertEmptyIdThrows(Action action, string parameterName, string typeName)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(action);

        Assert.Equal(parameterName, exception.ParamName);
        Assert.Contains(typeName, exception.Message, StringComparison.Ordinal);
    }
}

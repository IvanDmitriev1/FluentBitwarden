using BitwardenApi.Notifications.Internal;
using BitwardenApi.Notifications.Models;

namespace BitwardenApi.Tests.Tests.Notifications;

public sealed class NotificationPayloadSerializationTests
{
    [Fact]
    public void Notification_envelope_maps_a_null_context_id_to_default()
    {
        NotificationEnvelope envelope = JsonSerializer.Deserialize(
            """{"contextId":null,"type":1,"payload":{}}""",
            NotificationsJsonContext.ConfiguredDefault.NotificationEnvelope);

        Assert.Equal(default, envelope.ContextId);
    }

    [Fact]
    public void Cipher_payload_normalizes_absent_optional_ids_and_round_trips_real_fields()
    {
        const string json = """{"id":"cipher-1","organizationId":null,"collectionIds":["collection-1"],"revisionDate":"2026-10-28T12:34:56Z"}""";
        CipherChangedNotification value = JsonSerializer.Deserialize(json, NotificationsJsonContext.ConfiguredDefault.CipherChangedNotification);
        Assert.Equal(CipherId.Parse("cipher-1"), value.Id);
        Assert.Equal(UserId.Empty, value.UserId);
        Assert.Equal(OrganizationId.Empty, value.OrganizationId);
        Assert.Equal(CollectionId.Parse("collection-1"), Assert.Single(value.CollectionIds));
        Assert.Equal(DateTimeOffset.Parse("2026-10-28T12:34:56Z"), value.RevisionDate);

        string serialized = JsonSerializer.Serialize(value, NotificationsJsonContext.ConfiguredDefault.CipherChangedNotification);
        CipherChangedNotification roundTrip = JsonSerializer.Deserialize(serialized, NotificationsJsonContext.ConfiguredDefault.CipherChangedNotification);
        Assert.Equal(value.Id, roundTrip.Id);
        Assert.Equal(value.UserId, roundTrip.UserId);
        Assert.Equal(value.OrganizationId, roundTrip.OrganizationId);
        Assert.Equal(value.CollectionIds, roundTrip.CollectionIds);
        Assert.Equal(value.RevisionDate, roundTrip.RevisionDate);
    }

    [Fact]
    public void Cipher_payload_normalizes_explicit_null_ids_and_rejects_invalid_nonempty_ids()
    {
        const string nullIds = """{"id":"cipher-1","userId":null,"organizationId":null,"revisionDate":"2026-10-28T12:34:56Z"}""";
        CipherChangedNotification value = JsonSerializer.Deserialize(nullIds, NotificationsJsonContext.ConfiguredDefault.CipherChangedNotification);
        Assert.Equal(UserId.Empty, value.UserId);
        Assert.Equal(OrganizationId.Empty, value.OrganizationId);

        const string invalid = """{"id":"cipher-1","userId":"not-a-guid","revisionDate":"2026-10-28T12:34:56Z"}""";
        Assert.Throws<FormatException>(() => JsonSerializer.Deserialize(invalid, NotificationsJsonContext.ConfiguredDefault.CipherChangedNotification));
    }

    [Fact]
    public void Logout_payload_preserves_numeric_reason_and_allows_missing_reason()
    {
        const string withReason = """{"userId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","reason":1}""";
        LogOutNotification logout = JsonSerializer.Deserialize(withReason, NotificationsJsonContext.ConfiguredDefault.LogOutNotification);
        Assert.Equal(PushNotificationLogOutReasonType.KeyRotation, logout.Reason);

        const string withoutReason = """{"userId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"}""";
        Assert.Null(JsonSerializer.Deserialize(withoutReason, NotificationsJsonContext.ConfiguredDefault.LogOutNotification).Reason);
    }

    [Fact]
    public void Other_upstream_payloads_deserialize_their_distinct_fields()
    {
        SyncSendNotification send = Read<SyncSendNotification>("""{"id":"send-1","userId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","revisionDate":"2026-10-28T12:34:56Z"}""", NotificationsJsonContext.ConfiguredDefault.SyncSendNotification);
        Assert.Equal("send-1", send.Id);
        Assert.Equal(DateTimeOffset.Parse("2026-10-28T12:34:56Z"), send.RevisionDate);

        AuthRequestPushNotification auth = Read<AuthRequestPushNotification>("""{"id":"auth-1","userId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"}""", NotificationsJsonContext.ConfiguredDefault.AuthRequestPushNotification);
        Assert.Equal(AuthRequestId.Parse("auth-1"), auth.Id);

        OrganizationStatusPushNotification status = Read<OrganizationStatusPushNotification>("""{"organizationId":"bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb","enabled":true}""", NotificationsJsonContext.ConfiguredDefault.OrganizationStatusPushNotification);
        Assert.True(status.Enabled);

        OrganizationCollectionSettingChangedPushNotification settings = Read<OrganizationCollectionSettingChangedPushNotification>("""{"organizationId":"bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb","limitCollectionCreation":true,"limitCollectionDeletion":false}""", NotificationsJsonContext.ConfiguredDefault.OrganizationCollectionSettingChangedPushNotification);
        Assert.True(settings.LimitCollectionCreation);
        Assert.False(settings.LimitCollectionDeletion);

        OrganizationBankAccountVerifiedPushNotification organizationBank = Read<OrganizationBankAccountVerifiedPushNotification>("""{"organizationId":"bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"}""", NotificationsJsonContext.ConfiguredDefault.OrganizationBankAccountVerifiedPushNotification);
        Assert.Equal(OrganizationId.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), organizationBank.OrganizationId);

        ProviderBankAccountVerifiedPushNotification providerBank = Read<ProviderBankAccountVerifiedPushNotification>("""{"providerId":"provider-1","adminId":"admin-1"}""", NotificationsJsonContext.ConfiguredDefault.ProviderBankAccountVerifiedPushNotification);
        Assert.Equal("provider-1", providerBank.ProviderId);
        Assert.Equal("admin-1", providerBank.AdminId);

        AutoConfirmMemberNotification autoConfirm = Read<AutoConfirmMemberNotification>("""{"userId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","targetUserId":"cccccccc-cccc-cccc-cccc-cccccccccccc","organizationId":"bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb","targetOrganizationUserId":"org-user-1"}""", NotificationsJsonContext.ConfiguredDefault.AutoConfirmMemberNotification);
        Assert.Equal(UserId.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), autoConfirm.TargetUserId);
        Assert.Equal("org-user-1", autoConfirm.TargetOrganizationUserId);

        PremiumStatusChangedNotification premium = Read<PremiumStatusChangedNotification>("""{"userId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","premium":true}""", NotificationsJsonContext.ConfiguredDefault.PremiumStatusChangedNotification);
        Assert.True(premium.Premium);

    }

    [Fact]
    public void Folder_and_vault_payloads_match_upstream_shapes()
    {
        FolderChangedNotification folder = Read<FolderChangedNotification>("""{"id":"folder-1","userId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","revisionDate":"2026-10-28T12:34:56Z"}""", NotificationsJsonContext.ConfiguredDefault.FolderChangedNotification);
        Assert.Equal(DateTimeOffset.Parse("2026-10-28T12:34:56Z"), folder.RevisionDate);
        VaultSyncRequestedNotification vault = Read<VaultSyncRequestedNotification>("""{"userId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","date":"2026-10-28T12:34:56Z"}""", NotificationsJsonContext.ConfiguredDefault.VaultSyncRequestedNotification);
        Assert.Equal(DateTimeOffset.Parse("2026-10-28T12:34:56Z"), vault.Date);
    }

    private static T Read<T>(string json, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo) =>
        JsonSerializer.Deserialize(json, typeInfo)!;
}

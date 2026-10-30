using BitwardenApi.Notifications;
using BitwardenApi.Notifications.Abstractions;
using BitwardenApi.Notifications.Internal;
using BitwardenApi.Notifications.Models;
using Microsoft.AspNetCore.SignalR.Client;
using NSubstitute;

namespace BitwardenApi.Tests.Tests.Notifications;

public sealed class NotificationsDispatchTests
{
    private const string CipherPayload = """{"id":"cipher-1","userId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","organizationId":"bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb","collectionIds":["collection-1"],"revisionDate":"2026-10-28T12:34:56Z"}""";
    private const string FolderPayload = """{"id":"folder-1","userId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","revisionDate":"2026-10-28T12:34:56Z"}""";
    private const string VaultSyncPayload = """{"userId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","date":"2026-10-28T12:34:56Z"}""";

    [Theory]
    [InlineData(NotificationType.SyncCipherCreate)]
    [InlineData(NotificationType.SyncCipherUpdate)]
    [InlineData(NotificationType.SyncCipherDelete)]
    [InlineData(NotificationType.SyncLoginDelete)]
    public async Task Dispatch_cipher_changes_to_the_cipher_handler(NotificationType type)
    {
        var handler = Substitute.For<INotificationHandler<CipherChangedNotification>>();
        var expected = new CipherChangedNotification(CipherId.Parse("cipher-1"), UserId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), OrganizationId.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), [CollectionId.Parse("collection-1")], DateTimeOffset.Parse("2026-10-28T12:34:56Z"));
        using ServiceProvider services = BuildServices(handler);
        await using var api = CreateClient(services);

        await api.DispatchAsync(Envelope(type, CipherPayload));

        await handler.Received(1).HandleAsync(type, Arg.Is<CipherChangedNotification>(actual =>
            actual.Id == expected.Id && actual.UserId == expected.UserId &&
            actual.OrganizationId == expected.OrganizationId &&
            actual.CollectionIds.SequenceEqual(expected.CollectionIds) &&
            actual.RevisionDate == expected.RevisionDate), CancellationToken.None);
    }

    [Fact]
    public async Task Dispatch_folder_changes_to_folder_handler()
    {
        var handler = Substitute.For<INotificationHandler<FolderChangedNotification>>();
        var expected = new FolderChangedNotification(FolderId.Parse("folder-1"), UserId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), DateTimeOffset.Parse("2026-10-28T12:34:56Z"));
        using ServiceProvider services = BuildServices(handler);
        await using var api = CreateClient(services);

        await api.DispatchAsync(Envelope(NotificationType.SyncFolderUpdate, FolderPayload));

        await handler.Received(1).HandleAsync(NotificationType.SyncFolderUpdate, expected, CancellationToken.None);
    }

    [Theory]
    [InlineData(NotificationType.SyncVault)]
    [InlineData(NotificationType.SyncCiphers)]
    [InlineData(NotificationType.SyncOrgKeys)]
    [InlineData(NotificationType.SyncSettings)]
    [InlineData(NotificationType.SyncOrganizations)]
    [InlineData(NotificationType.SyncPolicy)]
    public async Task Dispatch_user_sync_types_to_vault_sync_handler(NotificationType type)
    {
        var handler = Substitute.For<INotificationHandler<VaultSyncRequestedNotification>>();
        var expected = new VaultSyncRequestedNotification(UserId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), DateTimeOffset.Parse("2026-10-28T12:34:56Z"));
        using ServiceProvider services = BuildServices(handler);
        await using var api = CreateClient(services);
        await api.DispatchAsync(Envelope(type, VaultSyncPayload));
        await handler.Received(1).HandleAsync(type, expected, CancellationToken.None);
    }

    [Fact]
    public async Task Dispatch_organization_status_to_its_payload_type()
    {
        var handler = Substitute.For<INotificationHandler<OrganizationStatusPushNotification>>();
        var expected = new OrganizationStatusPushNotification(OrganizationId.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), true);
        using ServiceProvider services = BuildServices(handler);
        await using var api = CreateClient(services);
        await api.DispatchAsync(Envelope(NotificationType.SyncOrganizationStatusChanged, """{"organizationId":"bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb","enabled":true}"""));
        await handler.Received(1).HandleAsync(NotificationType.SyncOrganizationStatusChanged, expected, CancellationToken.None);
    }

    [Fact]
    public async Task Dispatch_organization_collection_settings_to_their_payload_type()
    {
        var handler = Substitute.For<INotificationHandler<OrganizationCollectionSettingChangedPushNotification>>();
        var expected = new OrganizationCollectionSettingChangedPushNotification(OrganizationId.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), true, false);
        using ServiceProvider services = BuildServices(handler);
        await using var api = CreateClient(services);
        await api.DispatchAsync(Envelope(NotificationType.SyncOrganizationCollectionSettingChanged, """{"organizationId":"bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb","limitCollectionCreation":true,"limitCollectionDeletion":false}"""));
        await handler.Received(1).HandleAsync(NotificationType.SyncOrganizationCollectionSettingChanged, expected, CancellationToken.None);
    }

    [Fact]
    public async Task Dispatch_new_notification_types_to_their_payload_models()
    {
        const string userId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
        const string orgId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
        var logout = Substitute.For<INotificationHandler<LogOutNotification>>();
        var sendHandler = Substitute.For<INotificationHandler<SyncSendNotification>>();
        var authHandler = Substitute.For<INotificationHandler<AuthRequestPushNotification>>();
        var orgVerifiedHandler = Substitute.For<INotificationHandler<OrganizationBankAccountVerifiedPushNotification>>();
        var providerVerifiedHandler = Substitute.For<INotificationHandler<ProviderBankAccountVerifiedPushNotification>>();
        var autoConfirmHandler = Substitute.For<INotificationHandler<AutoConfirmMemberNotification>>();
        var premiumHandler = Substitute.For<INotificationHandler<PremiumStatusChangedNotification>>();
        using ServiceProvider services = new ServiceCollection()
            .AddSingleton(logout).AddSingleton(sendHandler).AddSingleton(authHandler)
            .AddSingleton(orgVerifiedHandler).AddSingleton(providerVerifiedHandler)
            .AddSingleton(autoConfirmHandler).AddSingleton(premiumHandler).BuildServiceProvider();
        await using var api = CreateClient(services);

        await api.DispatchAsync(Envelope(NotificationType.LogOut, $"{{\"userId\":\"{userId}\",\"reason\":0}}"));
        await logout.Received(1).HandleAsync(NotificationType.LogOut, new LogOutNotification(UserId.Parse(userId), PushNotificationLogOutReasonType.KdfChange), CancellationToken.None);

        const string send = """{"id":"send-1","userId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","revisionDate":"2026-10-28T12:34:56Z"}""";
        foreach (NotificationType type in new[] { NotificationType.SyncSendCreate, NotificationType.SyncSendUpdate, NotificationType.SyncSendDelete })
        {
            await api.DispatchAsync(Envelope(type, send));
            await sendHandler.Received(1).HandleAsync(type, Arg.Any<SyncSendNotification>(), CancellationToken.None);
        }

        const string auth = """{"id":"auth-1","userId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"}""";
        await api.DispatchAsync(Envelope(NotificationType.AuthRequest, auth));
        await api.DispatchAsync(Envelope(NotificationType.AuthRequestResponse, auth));
        await authHandler.Received(1).HandleAsync(NotificationType.AuthRequest, Arg.Any<AuthRequestPushNotification>(), CancellationToken.None);
        await authHandler.Received(1).HandleAsync(NotificationType.AuthRequestResponse, Arg.Any<AuthRequestPushNotification>(), CancellationToken.None);

        await api.DispatchAsync(Envelope(NotificationType.OrganizationBankAccountVerified, $"{{\"organizationId\":\"{orgId}\"}}"));
        await orgVerifiedHandler.Received(1).HandleAsync(NotificationType.OrganizationBankAccountVerified, Arg.Any<OrganizationBankAccountVerifiedPushNotification>(), CancellationToken.None);

        await api.DispatchAsync(Envelope(NotificationType.ProviderBankAccountVerified, """{"providerId":"provider-1","adminId":"admin-1"}"""));
        await providerVerifiedHandler.Received(1).HandleAsync(NotificationType.ProviderBankAccountVerified, Arg.Any<ProviderBankAccountVerifiedPushNotification>(), CancellationToken.None);

        await api.DispatchAsync(Envelope(NotificationType.AutoConfirmMember, $"{{\"userId\":\"{userId}\",\"targetUserId\":\"{userId}\",\"organizationId\":\"{orgId}\",\"targetOrganizationUserId\":\"member-1\"}}"));
        await autoConfirmHandler.Received(1).HandleAsync(NotificationType.AutoConfirmMember, Arg.Any<AutoConfirmMemberNotification>(), CancellationToken.None);

        await api.DispatchAsync(Envelope(NotificationType.PremiumStatusChanged, $"{{\"userId\":\"{userId}\",\"premium\":true}}"));
        await premiumHandler.Received(1).HandleAsync(NotificationType.PremiumStatusChanged, Arg.Any<PremiumStatusChangedNotification>(), CancellationToken.None);
    }

    [Theory]
    [InlineData((NotificationType)byte.MaxValue)]
    [InlineData(NotificationType.Notification)]
    [InlineData(NotificationType.NotificationStatus)]
    [InlineData(NotificationType.RefreshSecurityTasks)]
    [InlineData(NotificationType.RefreshApproverInbox)]
    [InlineData(NotificationType.RefreshAccessRequest)]
    public async Task Dispatch_unknown_and_unsupported_types_without_handlers(NotificationType type)
    {
        using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
        await using var dispatcher = CreateClient(services);
        await dispatcher.DispatchAsync(new NotificationEnvelope(default, type, default));
    }

    [Fact]
    public async Task Dispatch_rejects_invalid_payload_and_propagates_handler_failure()
    {
        var handler = Substitute.For<INotificationHandler<VaultSyncRequestedNotification>>();
        using ServiceProvider services = BuildServices(handler);
        await using var api = CreateClient(services);
        await Assert.ThrowsAsync<JsonException>(() => api.DispatchAsync(Envelope(NotificationType.SyncCipherCreate, "[]")));

        var expected = new InvalidOperationException("handler failure");
        handler.HandleAsync(Arg.Any<NotificationType>(), Arg.Any<VaultSyncRequestedNotification>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(expected));
        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() => api.DispatchAsync(Envelope(NotificationType.SyncVault, VaultSyncPayload)));
        Assert.Same(expected, actual);
    }

    private static ServiceProvider BuildServices<T>(INotificationHandler<T> handler) =>
        new ServiceCollection().AddSingleton(handler).BuildServiceProvider();

    private static ServerNotificationsDispatcher CreateClient(IServiceProvider services) =>
        new(new HubConnectionBuilder().WithUrl("http://localhost").Build(), services.GetRequiredService<IServiceScopeFactory>(), TimeProvider.System);

    private static NotificationEnvelope Envelope(NotificationType type, string payload)
    {
        using JsonDocument document = JsonDocument.Parse(payload);
        return new NotificationEnvelope(default, type, document.RootElement.Clone());
    }
}

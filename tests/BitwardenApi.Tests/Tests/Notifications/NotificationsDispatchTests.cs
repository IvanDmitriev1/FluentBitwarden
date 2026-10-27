using BitwardenApi.Notifications;
using BitwardenApi.Notifications.Internal;
using BitwardenApi.Notifications.Models;
using NSubstitute;

namespace BitwardenApi.Tests.Tests.Notifications;

public sealed class NotificationsDispatchTests
{
    private const string CipherPayload = """
        {"id":"cipher-1","userId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","isDeleted":true}
        """;

    private const string FolderPayload = """
        {"id":"folder-1","userId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","isDeleted":false}
        """;

    private const string VaultSyncPayload = """
        {"userId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"}
        """;

    [Theory]
    [InlineData(NotificationType.SyncCipherCreate)]
    [InlineData(NotificationType.SyncCipherUpdate)]
    [InlineData(NotificationType.SyncCipherDelete)]
    public async Task Dispatch_cipher_changes_to_the_cipher_handler(NotificationType type)
    {
        var handler = Substitute.For<INotificationHandler<CipherChangedNotification>>();
        handler.HandleAsync(Arg.Any<CipherChangedNotification>()).Returns(Task.CompletedTask);
        using ServiceProvider services = new ServiceCollection().AddSingleton(handler).BuildServiceProvider();
        await using var api = new NotificationsApi(Substitute.For<IAccessTokenProvider>(), services);

        await api.DispatchAsync(Envelope(type, CipherPayload));

        await handler.Received(1).HandleAsync(new CipherChangedNotification(
            CipherId.Parse("cipher-1"),
            UserId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            true));
    }

    [Theory]
    [InlineData(NotificationType.SyncFolderCreate)]
    [InlineData(NotificationType.SyncFolderUpdate)]
    [InlineData(NotificationType.SyncFolderDelete)]
    public async Task Dispatch_folder_changes_to_the_folder_handler(NotificationType type)
    {
        var handler = Substitute.For<INotificationHandler<FolderChangedNotification>>();
        handler.HandleAsync(Arg.Any<FolderChangedNotification>()).Returns(Task.CompletedTask);
        using ServiceProvider services = new ServiceCollection().AddSingleton(handler).BuildServiceProvider();
        await using var api = new NotificationsApi(Substitute.For<IAccessTokenProvider>(), services);

        await api.DispatchAsync(Envelope(type, FolderPayload));

        await handler.Received(1).HandleAsync(new FolderChangedNotification(
            FolderId.Parse("folder-1"),
            UserId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            false));
    }

    [Theory]
    [InlineData(NotificationType.SyncVault)]
    [InlineData(NotificationType.SyncCiphers)]
    [InlineData(NotificationType.SyncOrgKeys)]
    [InlineData(NotificationType.SyncSettings)]
    [InlineData(NotificationType.SyncOrganizations)]
    [InlineData(NotificationType.SyncOrganizationStatusChanged)]
    [InlineData(NotificationType.SyncOrganizationCollectionSettingChanged)]
    [InlineData(NotificationType.SyncPolicy)]
    public async Task Dispatch_vault_sync_types_to_the_vault_sync_handler(NotificationType type)
    {
        var handler = Substitute.For<INotificationHandler<VaultSyncRequestedNotification>>();
        handler.HandleAsync(Arg.Any<VaultSyncRequestedNotification>()).Returns(Task.CompletedTask);
        using ServiceProvider services = new ServiceCollection().AddSingleton(handler).BuildServiceProvider();
        await using var api = new NotificationsApi(Substitute.For<IAccessTokenProvider>(), services);

        await api.DispatchAsync(Envelope(type, VaultSyncPayload));

        await handler.Received(1).HandleAsync(new VaultSyncRequestedNotification(
            UserId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")));
    }

    [Theory]
    [InlineData(NotificationType.SyncLoginDelete)]
    [InlineData(NotificationType.LogOut)]
    [InlineData(NotificationType.SyncSendCreate)]
    [InlineData(NotificationType.SyncSendUpdate)]
    [InlineData(NotificationType.SyncSendDelete)]
    [InlineData(NotificationType.AuthRequest)]
    [InlineData(NotificationType.AuthRequestResponse)]
    [InlineData(NotificationType.Notification)]
    [InlineData(NotificationType.NotificationStatus)]
    [InlineData(NotificationType.RefreshSecurityTasks)]
    [InlineData(NotificationType.OrganizationBankAccountVerified)]
    [InlineData(NotificationType.ProviderBankAccountVerified)]
    [InlineData(NotificationType.AutoConfirmMember)]
    [InlineData((NotificationType)byte.MaxValue)]
    public async Task Dispatch_unsupported_and_unknown_types_without_handlers(NotificationType type)
    {
        using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
        await using var api = new NotificationsApi(Substitute.For<IAccessTokenProvider>(), services);

        await api.DispatchAsync(new NotificationEnvelope(default, type, default));
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("null")]
    public async Task Dispatch_rejects_a_payload_that_does_not_match_the_notification_shape(string payload)
    {
        using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
        await using var api = new NotificationsApi(Substitute.For<IAccessTokenProvider>(), services);

        await Assert.ThrowsAsync<JsonException>(() => api.DispatchAsync(Envelope(
            NotificationType.SyncCipherCreate,
            payload)));
    }

    [Fact]
    public async Task Dispatch_requires_a_handler_for_a_supported_notification()
    {
        using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
        await using var api = new NotificationsApi(Substitute.For<IAccessTokenProvider>(), services);

        await Assert.ThrowsAsync<InvalidOperationException>(() => api.DispatchAsync(Envelope(
            NotificationType.SyncVault,
            VaultSyncPayload)));
    }

    [Fact]
    public async Task Dispatch_propagates_a_handler_failure()
    {
        var expected = new InvalidOperationException("handler failure");
        var handler = Substitute.For<INotificationHandler<VaultSyncRequestedNotification>>();
        handler.HandleAsync(Arg.Any<VaultSyncRequestedNotification>()).Returns(Task.FromException(expected));
        using ServiceProvider services = new ServiceCollection().AddSingleton(handler).BuildServiceProvider();
        await using var api = new NotificationsApi(Substitute.For<IAccessTokenProvider>(), services);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => api.DispatchAsync(Envelope(NotificationType.SyncVault, VaultSyncPayload)));

        Assert.Same(expected, actual);
    }

    private static NotificationEnvelope Envelope(NotificationType type, string payload)
    {
        using JsonDocument document = JsonDocument.Parse(payload);
        return new NotificationEnvelope(default, type, document.RootElement.Clone());
    }
}

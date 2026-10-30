using BitwardenApi.Notifications.Abstractions;
using BitwardenApi.Vault.Sync;
using BitwardenApi.Vault.Ciphers;
using BitwardenApi.Vault.Folders;
using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Persistence;
using FluentBitwarden.AppHost.Modules.Vault.Services;
using FluentBitwarden.Platform.Infrastructure.Connectivity;
using FluentBitwarden.Platform.Ipc.Abstractions;
using Dapper;

namespace FluentBitwarden.AppHost.IntegrationTests.Modules.Vault;

internal sealed class VaultManagerTestContext : IDisposable
{
    private readonly AccountRepositoryTestDatabase _database = new();
    private readonly UnitOfWork _unitOfWork;
    private readonly VaultWriterRepository _vaultWriterRepository;
    private readonly VaultSyncStateRepository _vaultSyncStateRepository;

    public VaultManagerTestContext()
    {
        Account = AccountTestData.Profile(AccountTestData.FirstUserId, "vault@example.test", "first");
        AccountRepositoryTestHelper.InsertAccount(_database, Account);
        _unitOfWork = _database.CreateUnitOfWork();
        _vaultWriterRepository = new VaultWriterRepository(_unitOfWork);
        _vaultSyncStateRepository = new VaultSyncStateRepository(_unitOfWork);
        SyncApi = Substitute.For<IVaultSyncApi>();
        CiphersApi = Substitute.For<ICiphersApi>();
        FoldersApi = Substitute.For<IFoldersApi>();
        EventPublisher = Substitute.For<IIpcEventPublisher>();
        NetworkStatus = Substitute.For<INetworkStatus>();
        NetworkStatus.HasInternetAccess.Returns(true);

        VaultReaderRepository = new VaultReaderRepository(_unitOfWork);
        VaultService = new VaultService(
            SessionAccessor,
            VaultReaderRepository,
            _vaultWriterRepository,
            _vaultSyncStateRepository,
            SyncApi,
            CiphersApi,
            FoldersApi,
            _unitOfWork,
            NetworkStatus,
            EventPublisher);
    }

    public IAppSessionAccessor SessionAccessor { get; } = Substitute.For<IAppSessionAccessor>();
    public AccountProfile Account { get; }
    public BitwardenAccountContext AccountContext => Account.BitwardenAccountContext;
    public IVaultSyncApi SyncApi { get; }
    public ICiphersApi CiphersApi { get; }
    public IFoldersApi FoldersApi { get; }
    public IIpcEventPublisher EventPublisher { get; }
    public INetworkStatus NetworkStatus { get; }
    public VaultReaderRepository VaultReaderRepository { get; }
    public VaultService VaultService { get; }

    public VaultManagerTestKeySession CreateKeySession() => new(AccountContext.UserId);

    public IAppSessionAccessor CreateLease(IUnlockedVault vault, IAccountKeySession keySession)
    {
        var state = new AppSessionState.Unlocked(Account, vault, keySession, Substitute.For<IServerNotificationsClient>());
        SessionAccessor.AppSessionState.Returns(state);
        return SessionAccessor;
    }

    public IUnlockedVault OpenVault()
    {
        using var keySession = CreateKeySession();
        return VaultService.Open(AccountContext, keySession);
    }

    public IUnlockedVault OpenVaultFromFreshConnection(IAccountKeySession keySession)
    {
        using var unitOfWork = _database.CreateUnitOfWork();
        return new VaultService(
            SessionAccessor,
            new VaultReaderRepository(unitOfWork),
            _vaultWriterRepository,
            _vaultSyncStateRepository,
            SyncApi,
            CiphersApi,
            FoldersApi,
            unitOfWork,
            NetworkStatus,
            EventPublisher).Open(AccountContext, keySession);
    }

    public void SeedCache(
        CipherResponse[]? ciphers = null,
        FolderResponse[]? folders = null,
        DateTimeOffset? serverRevisionDate = null,
        VaultOrganizationResponse[]? organizations = null,
        CollectionResponse[]? collections = null)
    {
        _unitOfWork.Begin();
        _vaultWriterRepository.WriteOrganizations(AccountContext.UserId, organizations ?? []);
        _vaultWriterRepository.WriteFolders(AccountContext.UserId, folders ?? []);
        _vaultWriterRepository.WriteCollections(AccountContext.UserId, collections ?? []);
        _vaultWriterRepository.WriteCiphers(AccountContext.UserId, ciphers ?? []);
        if (serverRevisionDate is { } revisionDate)
            _vaultSyncStateRepository.UpsertServerRevisionDate(AccountContext.UserId, revisionDate);
        _unitOfWork.Commit();
    }

    public DateTimeOffset? GetServerRevisionDate() =>
        _vaultSyncStateRepository.GetServerRevisionDate(AccountContext.UserId);

    public DateTimeOffset? GetServerRevisionDateFromFreshConnection()
    {
        using var unitOfWork = _database.CreateUnitOfWork();
        return new VaultSyncStateRepository(unitOfWork).GetServerRevisionDate(AccountContext.UserId);
    }

    public void FailCheckpointWrites()
    {
        using var connection = _database.OpenConnection();
        connection.Execute("""
            CREATE TRIGGER fail_checkpoint_writes BEFORE UPDATE ON vault_sync_state
            BEGIN SELECT RAISE(ABORT, 'synthetic checkpoint persistence failure'); END;
            """);
    }

    public void CloseUnitOfWork() => _unitOfWork.Dispose();

    public void Dispose()
    {
        _unitOfWork.Dispose();
        _database.Dispose();
    }
}

internal sealed class VaultManagerTestKeySession(UserId userId) : IAccountKeySession
{
    private static readonly byte[] KeyBytes = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();

    public UserId UserId => UserKey.UserId;
    public UnlockedUserKey UserKey { get; } = new(userId, (byte[])KeyBytes.Clone());

    public void UpdateOrganizations(IReadOnlyDictionary<OrganizationId, AsymmetricEncString> organizationKeys)
    {
        if (organizationKeys.Count != 0)
            throw new NotSupportedException("Organization keys are not part of these tests.");
    }

    public SymmetricCryptoKey GetDecryptionKey(OrganizationId organizationId)
    {
        if (!organizationId.IsEmpty)
            throw new NotSupportedException("Organization keys are not part of these tests.");

        return UserKey;
    }

    public AttachmentKey CreateAttachmentKey(
        OrganizationId organizationId,
        EncString protectedCipherKey,
        EncString protectedAttachmentKey) =>
        throw new NotSupportedException("Attachments are not part of these manager tests.");

    public void Dispose() => UserKey.Dispose();
}

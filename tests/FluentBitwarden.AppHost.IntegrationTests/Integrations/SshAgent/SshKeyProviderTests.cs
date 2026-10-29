using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.AppSession.Services;
using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;
using FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;
using FluentBitwarden.AppHost.Integrations.SshAgent.Services;
using FluentBitwarden.AppHost.Infrastructure.Processes;
using FluentBitwarden.Contracts.Integrations.Ssh;
using FluentBitwarden.Contracts.Modules.Ssh;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Search;
using FluentBitwarden.Contracts.Settings;
using FluentBitwarden.Contracts.Settings.Models;
using static FluentBitwarden.AppHost.IntegrationTests.Integrations.SshAgent.SshKeyProviderTestSupport;

namespace FluentBitwarden.AppHost.IntegrationTests.Integrations.SshAgent;

public sealed class SshKeyProviderTests
{
    [Fact]
    public async Task GetIdentities_observes_unlock_on_an_existing_connection()
    {
        var fixture = new ProviderFixture(SensitiveActionPolicy.AllowWhenUnlocked);
        var store = new AppSessionStore(Substitute.For<FluentBitwarden.Platform.Ipc.Abstractions.IIpcEventPublisher>());
        var cancellationToken = TestContext.Current.CancellationToken;
        await store.AuthenticateAsync(fixture.Account, cancellationToken);
        var provider = new SshKeyProvider(
            store, fixture.UiProcessLauncher, fixture.DialogClient, fixture.SettingsStore);
        Assert.Empty(provider.GetIdentities());

        await store.UnlockAsync(fixture.BeforeApprovalLease.RequireUnlockedState());

        SshAgentIdentity identity = Assert.Single(provider.GetIdentities());
        Assert.Equal(fixture.Key.PublicKeyBlob, identity.PublicKeyBlob.ToArray());
        await store.LockAsync(cancellationToken);
    }

    [Fact]
    public async Task GetIdentities_stops_exposing_keys_when_the_session_locks_on_an_existing_connection()
    {
        var fixture = new ProviderFixture(SensitiveActionPolicy.AllowWhenUnlocked);
        var store = new AppSessionStore(Substitute.For<FluentBitwarden.Platform.Ipc.Abstractions.IIpcEventPublisher>());
        var state = fixture.BeforeApprovalLease.RequireUnlockedState();
        var cancellationToken = TestContext.Current.CancellationToken;
        await store.AuthenticateAsync(fixture.Account, cancellationToken);
        await store.UnlockAsync(state);
        var provider = new SshKeyProvider(
            store, fixture.UiProcessLauncher, fixture.DialogClient, fixture.SettingsStore);
        Assert.Single(provider.GetIdentities());

        await store.LockAsync(cancellationToken);

        Assert.Empty(provider.GetIdentities());
    }

    [Fact]
    public void GetIdentities_returns_no_keys_while_the_session_is_locked()
    {
        var fixture = new ProviderFixture(SensitiveActionPolicy.AllowWhenUnlocked);
        fixture.Accessor.AppSessionState.Returns(new AppSessionState.Locked(fixture.Account));

        IReadOnlyList<SshAgentIdentity> identities = fixture.Provider.GetIdentities();

        Assert.Empty(identities);
        _ = fixture.Session.Received(1).CreateLease();
        fixture.Accessor.Received(1).Dispose();
    }

    [Fact]
    public void GetIdentities_returns_ssh_keys_from_an_unlocked_vault()
    {
        var fixture = new ProviderFixture(SensitiveActionPolicy.AllowWhenUnlocked);
        Cipher login = Cipher.CreateBlankCipher(CipherType.Login);
        fixture.ConfigureVault(fixture.Key.CreateCipher("work key"), login);

        IReadOnlyList<SshAgentIdentity> identities = fixture.Provider.GetIdentities();

        SshAgentIdentity identity = Assert.Single(identities);
        Assert.Equal(fixture.Key.PublicKeyBlob, identity.PublicKeyBlob.ToArray());
        Assert.Equal("work key", identity.Comment);
    }

    [Fact]
    public async Task GetSignature_returns_no_key_when_not_authenticated()
    {
        var fixture = new ProviderFixture(SensitiveActionPolicy.AllowWhenUnlocked);
        fixture.Accessor.AppSessionState.Returns(new AppSessionState.NotAuthenticated());

        SshAgentSignature? signature = await fixture.Provider.GetSignatureAsync(
            fixture.CreateRequest(),
            CancellationToken.None);

        Assert.Null(signature);
        _ = fixture.Session.DidNotReceive().WaitUntilUnlockedAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSignature_waits_for_a_locked_session_to_unlock_before_signing()
    {
        var fixture = new ProviderFixture(SensitiveActionPolicy.AllowWhenUnlocked);
        fixture.Accessor.AppSessionState.Returns(new AppSessionState.Locked(fixture.Account));
        var leaseSource = new TaskCompletionSource<IAppSessionAccessor>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Session.WaitUntilUnlockedAsync(Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<IAppSessionAccessor>(leaseSource.Task));
        byte[] data = "locked signing input"u8.ToArray();

        Task<SshAgentSignature?> signingTask = fixture.Provider.GetSignatureAsync(
            fixture.CreateRequest(data),
            CancellationToken.None).AsTask();

        Assert.False(signingTask.IsCompleted);
        leaseSource.SetResult(fixture.BeforeApprovalLease);
        SshAgentSignature? signature = await signingTask;

        Assert.NotNull(signature);
        AssertSignature(fixture.Key, data, signature);
        _ = fixture.Session.Received(1).WaitUntilUnlockedAsync(Arg.Any<CancellationToken>());
        fixture.BeforeApprovalLease.Received(1).Dispose();
    }

    [Fact]
    public async Task GetSignature_propagates_cancellation_while_waiting_for_unlock()
    {
        var fixture = new ProviderFixture(SensitiveActionPolicy.AllowWhenUnlocked);
        fixture.Accessor.AppSessionState.Returns(new AppSessionState.Locked(fixture.Account));
        var leaseSource = new TaskCompletionSource<IAppSessionAccessor>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Session.WaitUntilUnlockedAsync(Arg.Any<CancellationToken>())
            .Returns(call => new ValueTask<IAppSessionAccessor>(
                leaseSource.Task.WaitAsync(call.Arg<CancellationToken>())));
        using var cancellation = new CancellationTokenSource();

        Task<SshAgentSignature?> signingTask = fixture.Provider.GetSignatureAsync(
            fixture.CreateRequest(),
            cancellation.Token).AsTask();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => signingTask);
        _ = fixture.DialogClient.DidNotReceive().ShowSshDialogAsync(
            Arg.Any<SshUserActionRequest>(),
            Arg.Any<CancellationToken>());
        fixture.BeforeApprovalLease.DidNotReceive().Dispose();
    }

    [Fact]
    public async Task GetSignature_signs_without_prompt_when_policy_allows_unlocked_use()
    {
        var fixture = new ProviderFixture(SensitiveActionPolicy.AllowWhenUnlocked);
        byte[] data = "synthetic signing input"u8.ToArray();

        SshAgentSignature? signature = await fixture.Provider.GetSignatureAsync(
            fixture.CreateRequest(data),
            CancellationToken.None);

        Assert.NotNull(signature);
        AssertSignature(fixture.Key, data, signature);
        _ = fixture.DialogClient.DidNotReceive().ShowSshDialogAsync(
            Arg.Any<SshUserActionRequest>(),
            Arg.Any<CancellationToken>());
        fixture.UiProcessLauncher.Received(1).Activate();
    }

    [Fact]
    public async Task GetSignature_prompts_and_signs_after_user_approval()
    {
        var fixture = new ProviderFixture(SensitiveActionPolicy.RequireUserAction);
        fixture.DialogClient.ShowSshDialogAsync(
                Arg.Any<SshUserActionRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                fixture.BeforeApprovalLease.Received(1).Dispose();
                return Task.FromResult(UserActionDialogOutcome.Approved);
            });
        byte[] data = "approved signing input"u8.ToArray();

        SshAgentSignature? signature = await fixture.Provider.GetSignatureAsync(
            fixture.CreateRequest(data),
            CancellationToken.None);

        Assert.NotNull(signature);
        AssertSignature(fixture.Key, data, signature);
        _ = fixture.DialogClient.Received(1).ShowSshDialogAsync(
            new SshUserActionRequest("synthetic key", "SHA256:synthetic", false),
            Arg.Any<CancellationToken>());
        fixture.ActiveLease.Received(1).Dispose();
    }

    [Fact]
    public async Task GetSignature_returns_no_signature_after_user_denial()
    {
        var fixture = new ProviderFixture(SensitiveActionPolicy.RequireUserAction);
        fixture.DialogClient.ShowSshDialogAsync(
                Arg.Any<SshUserActionRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                fixture.BeforeApprovalLease.Received(1).Dispose();
                return Task.FromResult(UserActionDialogOutcome.Denied);
            });

        SshAgentSignature? signature = await fixture.Provider.GetSignatureAsync(
            fixture.CreateRequest(),
            CancellationToken.None);

        Assert.Null(signature);
        fixture.ActiveLease.DidNotReceive().Dispose();
    }

    [Fact]
    public async Task GetSignature_returns_no_signature_if_the_session_relocks_during_approval()
    {
        var fixture = new ProviderFixture(SensitiveActionPolicy.RequireUserAction);
        var lockedLease = Substitute.For<IAppSessionAccessor>();
        lockedLease.AppSessionState.Returns(new AppSessionState.Locked(fixture.Account));
        fixture.SetActiveLease(lockedLease);
        fixture.DialogClient.ShowSshDialogAsync(
                Arg.Any<SshUserActionRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                fixture.BeforeApprovalLease.Received(1).Dispose();
                return Task.FromResult(UserActionDialogOutcome.Approved);
            });

        SshAgentSignature? signature = await fixture.Provider.GetSignatureAsync(
            fixture.CreateRequest(),
            CancellationToken.None);

        Assert.Null(signature);
        fixture.BeforeApprovalLease.Received(1).Dispose();
    }

    [Fact]
    public async Task GetSignature_returns_no_signature_if_the_active_account_changes_during_approval()
    {
        var fixture = new ProviderFixture(SensitiveActionPolicy.RequireUserAction);
        var otherAccount = AccountTestData.Profile(
            AccountTestData.SecondUserId,
            "other@example.test",
            "other");
        fixture.SetActiveLease(CreateLease(otherAccount, fixture.Key.CreateCipher()));
        fixture.AssertPreApprovalLeaseDisposedBeforeDialog();

        SshAgentSignature? signature = await fixture.Provider.GetSignatureAsync(
            fixture.CreateRequest(),
            CancellationToken.None);

        Assert.Null(signature);
        fixture.ActiveLease.Received(1).Dispose();
    }

    [Fact]
    public async Task GetSignature_returns_no_signature_if_the_approved_key_was_replaced()
    {
        var fixture = new ProviderFixture(SensitiveActionPolicy.RequireUserAction);
        SyntheticOpenSshKey replacementKey = SshAgentTestData.CreateKey(0x71);
        fixture.SetActiveLease(CreateLease(fixture.Account, replacementKey.CreateCipher()));
        fixture.AssertPreApprovalLeaseDisposedBeforeDialog();

        SshAgentSignature? signature = await fixture.Provider.GetSignatureAsync(
            fixture.CreateRequest(),
            CancellationToken.None);

        Assert.Null(signature);
        fixture.ActiveLease.Received(1).Dispose();
    }

    private sealed class ProviderFixture
    {
        public ProviderFixture(SensitiveActionPolicy policy)
        {
            Account = AccountTestData.Profile(
                AccountTestData.FirstUserId,
                "user@example.test",
                "first");
            BeforeApprovalLease = CreateLease(Account, Key.CreateCipher());
            ActiveLease = CreateLease(Account, Key.CreateCipher());
            var state = BeforeApprovalLease.AppSessionState;
            Accessor.AppSessionState.Returns(state);
            Session.WaitUntilUnlockedAsync(Arg.Any<CancellationToken>())
                .Returns(new ValueTask<IAppSessionAccessor>(BeforeApprovalLease));
            var leaseAcquisitions = 0;
            Session.CreateLease().Returns(_ => leaseAcquisitions++ == 0 ? Accessor : ActiveLease);
            SettingsStore.Get(AppSettingKeys.SshAgent.UserVerificationPolicyKey).Returns(policy);
            DialogClient.ShowSshDialogAsync(
                    Arg.Any<SshUserActionRequest>(),
                    Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(UserActionDialogOutcome.Approved));
            Provider = new SshKeyProvider(Session, UiProcessLauncher, DialogClient, SettingsStore);
        }

        public SyntheticOpenSshKey Key { get; } = SshAgentTestData.CreateKey();
        public AccountProfile Account { get; }
        public IAppSessionAccessor BeforeApprovalLease { get; }
        public IAppSessionAccessor ActiveLease { get; private set; }
        public IAppSessionLeaseProvider Session { get; } = Substitute.For<IAppSessionLeaseProvider>();
        public IAppSessionAccessor Accessor { get; } = Substitute.For<IAppSessionAccessor>();
        public IUiProcessLauncher UiProcessLauncher { get; } = Substitute.For<IUiProcessLauncher>();
        public ISshUserActionDialogClient DialogClient { get; } = Substitute.For<ISshUserActionDialogClient>();
        public ISettingsStore SettingsStore { get; } = Substitute.For<ISettingsStore>();
        public SshKeyProvider Provider { get; }

        public SshAgentSignRequest CreateRequest(byte[]? data = null) =>
            new(Key.PublicKeyBlob, data ?? [0x01, 0x02], SshAgentSignatureFlags.None);

        public void ConfigureVault(params Cipher[] ciphers)
        {
            BeforeApprovalLease.RequireUnlockedState().Vault.GetCiphers(Arg.Any<VaultCipherQuery>()).Returns(ciphers);
            ActiveLease.RequireUnlockedState().Vault.GetCiphers(Arg.Any<VaultCipherQuery>()).Returns(ciphers);
        }

        public void SetActiveLease(IAppSessionAccessor lease)
        {
            ActiveLease = lease;
        }

        public void AssertPreApprovalLeaseDisposedBeforeDialog() =>
            DialogClient.ShowSshDialogAsync(
                    Arg.Any<SshUserActionRequest>(),
                    Arg.Any<CancellationToken>())
                .Returns(_ =>
                {
                    Accessor.Received(1).Dispose();
                    BeforeApprovalLease.Received(1).Dispose();
                    return Task.FromResult(UserActionDialogOutcome.Approved);
                });

    }
}

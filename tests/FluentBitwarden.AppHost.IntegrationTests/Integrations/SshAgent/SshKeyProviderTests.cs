using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;
using FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;
using FluentBitwarden.AppHost.Integrations.SshAgent.Services;
using FluentBitwarden.AppHost.Infrastructure.Processes;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.Contracts.AppSession.State;
using FluentBitwarden.Contracts.Integrations.Ssh;
using FluentBitwarden.Contracts.Modules.Ssh;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Search;
using FluentBitwarden.Contracts.Settings;
using FluentBitwarden.Contracts.Settings.Models;
using NSubstitute;
using Org.BouncyCastle.Crypto.Signers;

namespace FluentBitwarden.AppHost.IntegrationTests.Integrations.SshAgent;

public sealed class SshKeyProviderTests
{
    [Fact]
    public void GetIdentities_returns_no_keys_while_the_session_is_locked()
    {
        var fixture = new ProviderFixture(SensitiveActionPolicy.AllowWhenUnlocked);
        fixture.Session.State.Returns(new AppSessionState.Locked(fixture.Account));

        IReadOnlyList<SshAgentIdentity> identities = fixture.Provider.GetIdentities();

        Assert.Empty(identities);
        _ = fixture.Session.DidNotReceive().RequireUnlockedSessionLease();
    }

    [Fact]
    public void GetIdentities_returns_ssh_keys_from_an_unlocked_vault()
    {
        var fixture = new ProviderFixture(SensitiveActionPolicy.AllowWhenUnlocked);
        VaultCipher login = VaultCipher.CreateBlankCipher(VaultCipherType.Login);
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
        fixture.Session.State.Returns(new AppSessionState.NotAuthenticated());

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
        fixture.Session.State.Returns(new AppSessionState.Locked(fixture.Account));
        var leaseSource = new TaskCompletionSource<IUnlockedSessionLease>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Session.WaitUntilUnlockedAsync(Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<IUnlockedSessionLease>(leaseSource.Task));
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
        fixture.Session.State.Returns(new AppSessionState.Locked(fixture.Account));
        var leaseSource = new TaskCompletionSource<IUnlockedSessionLease>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Session.WaitUntilUnlockedAsync(Arg.Any<CancellationToken>())
            .Returns(call => new ValueTask<IUnlockedSessionLease>(
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
        fixture.Session.RequireUnlockedSessionLease()
            .Returns(_ => throw new UnlockedSessionRequiredException());
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
        fixture.SetActiveLease(ProviderFixture.CreateLease(otherAccount, fixture.Key.CreateCipher()));
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
        fixture.SetActiveLease(ProviderFixture.CreateLease(fixture.Account, replacementKey.CreateCipher()));
        fixture.AssertPreApprovalLeaseDisposedBeforeDialog();

        SshAgentSignature? signature = await fixture.Provider.GetSignatureAsync(
            fixture.CreateRequest(),
            CancellationToken.None);

        Assert.Null(signature);
        fixture.ActiveLease.Received(1).Dispose();
    }

    private static void AssertSignature(SyntheticOpenSshKey key, byte[] data, SshAgentSignature signature)
    {
        Assert.Equal("ssh-ed25519", signature.Algorithm);
        var verifier = new Ed25519Signer();
        verifier.Init(false, key.PublicKey);
        verifier.BlockUpdate(data, 0, data.Length);
        Assert.True(verifier.VerifySignature(signature.Value));
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
            Session.State.Returns(new AppSessionState.Unlocked(Account));
            Session.WaitUntilUnlockedAsync(Arg.Any<CancellationToken>())
                .Returns(new ValueTask<IUnlockedSessionLease>(BeforeApprovalLease));
            Session.RequireUnlockedSessionLease().Returns(ActiveLease);
            SettingsStore.Get(AppSettingKeys.SshAgent.UserVerificationPolicyKey).Returns(policy);
            DialogClient.ShowSshDialogAsync(
                    Arg.Any<SshUserActionRequest>(),
                    Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(UserActionDialogOutcome.Approved));
            Provider = new SshKeyProvider(Session, UiProcessLauncher, DialogClient, SettingsStore);
        }

        public SyntheticOpenSshKey Key { get; } = SshAgentTestData.CreateKey();
        public AccountProfile Account { get; }
        public IUnlockedSessionLease BeforeApprovalLease { get; }
        public IUnlockedSessionLease ActiveLease { get; private set; }
        public IAppSessionService Session { get; } = Substitute.For<IAppSessionService>();
        public IUiProcessLauncher UiProcessLauncher { get; } = Substitute.For<IUiProcessLauncher>();
        public ISshUserActionDialogClient DialogClient { get; } = Substitute.For<ISshUserActionDialogClient>();
        public ISettingsStore SettingsStore { get; } = Substitute.For<ISettingsStore>();
        public SshKeyProvider Provider { get; }

        public SshAgentSignRequest CreateRequest(byte[]? data = null) =>
            new(Key.PublicKeyBlob, data ?? [0x01, 0x02], SshAgentSignatureFlags.None);

        public void ConfigureVault(params VaultCipher[] ciphers)
        {
            var vault = Substitute.For<IUnlockedVault>();
            vault.GetCiphers(Arg.Any<VaultCipherQuery>()).Returns(ciphers);
            BeforeApprovalLease.Vault.Returns(vault);
            ActiveLease.Vault.Returns(vault);
        }

        public void SetActiveLease(IUnlockedSessionLease lease)
        {
            ActiveLease = lease;
            Session.RequireUnlockedSessionLease().Returns(lease);
        }

        public void AssertPreApprovalLeaseDisposedBeforeDialog() =>
            DialogClient.ShowSshDialogAsync(
                    Arg.Any<SshUserActionRequest>(),
                    Arg.Any<CancellationToken>())
                .Returns(_ =>
                {
                    BeforeApprovalLease.Received(1).Dispose();
                    return Task.FromResult(UserActionDialogOutcome.Approved);
                });

        public static IUnlockedSessionLease CreateLease(AccountProfile account, params VaultCipher[] ciphers)
        {
            var vault = Substitute.For<IUnlockedVault>();
            vault.GetCiphers(Arg.Any<VaultCipherQuery>()).Returns(ciphers);
            var lease = Substitute.For<IUnlockedSessionLease>();
            lease.Account.Returns(account);
            lease.Vault.Returns(vault);
            return lease;
        }
    }
}

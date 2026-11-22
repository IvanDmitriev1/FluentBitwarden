using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.AppSession.Services;
using FluentBitwarden.AppHost.IntegrationTests.AppSession;
using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;
using FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;
using FluentBitwarden.AppHost.Integrations.SshAgent.Services;
using FluentBitwarden.Contracts.Modules.Ssh;
using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Search;
using FluentBitwarden.Contracts.Settings;
using FluentBitwarden.Contracts.Settings.Models;
using static FluentBitwarden.AppHost.IntegrationTests.Integrations.SshAgent.SshKeyProviderTestSupport;
using FluentBitwarden.Contracts.Infrastructure.UserDialog;
using FluentBitwarden.Contracts.Integrations.Ssh;

namespace FluentBitwarden.AppHost.IntegrationTests.Integrations.SshAgent;

public sealed class SshKeyProviderTests
{
    [Fact]
    public async Task GetIdentities_observes_unlock_on_an_existing_connection()
    {
        var fixture = new ProviderFixture(SensitiveActionPolicy.AllowWhenUnlocked);
        await using var session = new SessionTestContext();
        var cancellationToken = TestContext.Current.CancellationToken;
        await session.Store.AuthenticateAsync(fixture.Account, cancellationToken);
        await using var accessor = new AppSessionAccessor(session.Store);
        var provider = new SshKeyProvider(
            accessor, session.Operations, fixture.DialogService, fixture.SettingsStore);
        using var waitTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task<IReadOnlyList<SshAgentIdentity>> listing = provider.GetIdentitiesAsync(waitTimeout.Token).AsTask();
        Assert.False(listing.IsCompleted);
        await session.Store.UnlockAsync(fixture.State);
        SshAgentIdentity identity = Assert.Single(await listing);
        Assert.Equal(fixture.Key.PublicKeyBlob, identity.PublicKeyBlob.ToArray());
        await session.Operations.LockAsync(cancellationToken);
    }

    [Fact]
    public async Task GetIdentities_stops_exposing_keys_when_the_session_locks_on_an_existing_connection()
    {
        var fixture = new ProviderFixture(SensitiveActionPolicy.AllowWhenUnlocked);
        await using var session = new SessionTestContext();
        var cancellationToken = TestContext.Current.CancellationToken;
        await session.Store.AuthenticateAsync(fixture.Account, cancellationToken);
        await session.Store.UnlockAsync(fixture.State);
        await using var accessor = new AppSessionAccessor(session.Store);
        var provider = new SshKeyProvider(accessor, session.Operations, fixture.DialogService, fixture.SettingsStore);
        Assert.Single(await provider.GetIdentitiesAsync(cancellationToken));

        await session.Operations.LockAsync(cancellationToken);
        using var cancellation = new CancellationTokenSource();
        Task<IReadOnlyList<SshAgentIdentity>> listing = provider.GetIdentitiesAsync(cancellation.Token).AsTask();
        Assert.False(listing.IsCompleted);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => listing);
    }

    [Fact]
    public async Task GetIdentities_returns_ssh_keys_from_an_unlocked_vault()
    {
        var fixture = new ProviderFixture(SensitiveActionPolicy.AllowWhenUnlocked);
        Cipher login = Cipher.CreateBlankCipher(CipherType.Login);
        fixture.ConfigureVault(fixture.Key.CreateCipher("work key"), login);

        IReadOnlyList<SshAgentIdentity> identities = await fixture.Provider.GetIdentitiesAsync(TestContext.Current.CancellationToken);

        SshAgentIdentity identity = Assert.Single(identities);
        Assert.Equal(fixture.Key.PublicKeyBlob, identity.PublicKeyBlob.ToArray());
        Assert.Equal("work key", identity.Comment);
    }

    [Fact]
    public async Task GetSignature_requires_an_unlocked_injected_accessor()
    {
        var fixture = new ProviderFixture(SensitiveActionPolicy.AllowWhenUnlocked);
        fixture.Accessor.State.Returns(new AppSessionState.NotAuthenticated());

        await Assert.ThrowsAsync<UnlockedSessionRequiredException>(() => fixture.Provider.GetSignatureAsync(
            fixture.CreateRequest(),
            null,
            false,
            CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task GetSignature_signs_without_prompt_when_policy_allows_unlocked_use()
    {
        var fixture = new ProviderFixture(SensitiveActionPolicy.AllowWhenUnlocked);
        byte[] data = "synthetic signing input"u8.ToArray();

        SshAgentSignature? signature = await fixture.Provider.GetSignatureAsync(
            fixture.CreateRequest(data),
            null,
            false,
            CancellationToken.None);

        Assert.NotNull(signature);
        AssertSignature(fixture.Key, data, signature);
        _ = fixture.DialogService.DidNotReceive().ShowAsync<SshUserActionResponse, SshUserActionRequest>(
            Arg.Any<SshUserActionRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSignature_prompts_and_signs_after_user_approval()
    {
        var fixture = new ProviderFixture(SensitiveActionPolicy.RequireUserAction);
        byte[] data = "approved signing input"u8.ToArray();
        SshAgentSignature? signature = await fixture.Provider.GetSignatureAsync(
            fixture.CreateRequest(data),
            null,
            true,
            CancellationToken.None);

        Assert.NotNull(signature);
        AssertSignature(fixture.Key, data, signature);
        _ = fixture.DialogService.Received(1).ShowAsync<SshUserActionResponse, SshUserActionRequest>(
            new SshUserActionRequest("synthetic key", true, null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSignature_uses_the_username_from_a_valid_authentication_payload()
    {
        var fixture = new ProviderFixture(SensitiveActionPolicy.RequireUserAction);
        byte[] authPayload = SshAgentTestData.EncodeUserAuthRequest(
            [0x01, 0x02], "alice"u8, "publickey", fixture.Key.PublicKeyBlob);

        SshAgentSignature? signature = await fixture.Provider.GetSignatureAsync(
            fixture.CreateRequest(authPayload),
            null,
            false,
            CancellationToken.None);

        Assert.NotNull(signature);
        AssertSignature(fixture.Key, authPayload, signature);
        _ = fixture.DialogService.Received(1).ShowAsync<SshUserActionResponse, SshUserActionRequest>(
            new SshUserActionRequest("synthetic key", false, "alice"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSignature_uses_verified_binding_and_in_memory_known_hosts_for_approval_and_signing()
    {
        var fixture = new ProviderFixture(SensitiveActionPolicy.RequireUserAction);
        byte[] sessionId = [0x11, 0x22];
        (byte[] bindingPayload, byte[] hostKeyBlob) = SshAgentTestData.EncodeSessionBinding(
            sessionId, signingSeedByte: 0x72);
        Assert.True(SshAgentSessionBinding.TryParseAndVerify(bindingPayload, out var binding));
        byte[] authPayload = SshAgentTestData.EncodeUserAuthRequest(
            sessionId,
            "alice"u8,
            "publickey-hostbound-v00@openssh.com",
            fixture.Key.PublicKeyBlob,
            hostKeyBlob);
        var provider = new SshKeyProvider(
            fixture.Accessor,
            fixture.Operations,
            fixture.DialogService,
            fixture.SettingsStore,
            key =>
            {
                using var knownHosts = new StringReader(
                    $"build.example ssh-ed25519 {Convert.ToBase64String(hostKeyBlob)}");
                return SshAgentKnownHostsResolver.Resolve(key, knownHosts);
            });

        SshAgentSignature? signature = await provider.GetSignatureAsync(
            fixture.CreateRequest(authPayload), binding, false, CancellationToken.None);

        Assert.NotNull(signature);
        AssertSignature(fixture.Key, authPayload, signature);
        _ = fixture.DialogService.Received(1).ShowAsync<SshUserActionResponse, SshUserActionRequest>(
            new SshUserActionRequest("synthetic key", false, "alice@build.example"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSignature_returns_no_signature_after_user_denial()
    {
        var fixture = new ProviderFixture(SensitiveActionPolicy.RequireUserAction);
        fixture.DialogService.ShowAsync<SshUserActionResponse, SshUserActionRequest>(
                Arg.Any<SshUserActionRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                return UserDialogResult<SshUserActionResponse>.Completed(new SshUserActionResponse(false));
            });

        SshAgentSignature? signature = await fixture.Provider.GetSignatureAsync(
            fixture.CreateRequest(),
            null,
            false,
            CancellationToken.None);

        Assert.Null(signature);
    }

    [Fact]
    public async Task GetSignature_returns_no_signature_when_dialog_is_cancelled()
    {
        var fixture = new ProviderFixture(SensitiveActionPolicy.RequireUserAction);
        fixture.DialogService.ShowAsync<SshUserActionResponse, SshUserActionRequest>(
                Arg.Any<SshUserActionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(UserDialogResult<SshUserActionResponse>.Cancelled));

        SshAgentSignature? signature = await fixture.Provider.GetSignatureAsync(
            fixture.CreateRequest(), null, false, TestContext.Current.CancellationToken);

        Assert.Null(signature);
    }

    [Theory]
    [InlineData(SensitiveActionPolicy.AllowWhenUnlocked)]
    [InlineData(SensitiveActionPolicy.RequireUserAction)]
    public async Task GetSignature_returns_no_signature_when_state_is_already_revoked(SensitiveActionPolicy policy)
    {
        var fixture = new ProviderFixture(policy);
        fixture.State.AddReference();
        await fixture.State.DisposeAsync();

        try
        {
            Assert.Null(await fixture.Provider.GetSignatureAsync(fixture.CreateRequest(), null, false, CancellationToken.None));
            _ = fixture.DialogService.DidNotReceive().ShowAsync<SshUserActionResponse, SshUserActionRequest>(
                Arg.Any<SshUserActionRequest>(), Arg.Any<CancellationToken>());
        }
        finally
        {
            await fixture.State.ReleaseAsync();
        }
    }

    [Fact]
    public async Task GetSignature_rejects_approval_completed_after_state_revocation()
    {
        var fixture = new ProviderFixture(SensitiveActionPolicy.RequireUserAction);
        fixture.State.AddReference();
        fixture.DialogService.ShowAsync<SshUserActionResponse, SshUserActionRequest>(
                Arg.Any<SshUserActionRequest>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                await fixture.State.DisposeAsync();
                return UserDialogResult<SshUserActionResponse>.Completed(new SshUserActionResponse(true));
            });

        try
        {
            Assert.Null(await fixture.Provider.GetSignatureAsync(fixture.CreateRequest(), null, false, CancellationToken.None));
        }
        finally
        {
            await fixture.State.ReleaseAsync();
        }
    }

    [Theory]
    [InlineData("lock")]
    [InlineData("signout")]
    [InlineData("switch")]
    [InlineData("reunlock")]
    public async Task GetSignature_cancels_pending_approval_when_real_session_retires_captured_state(string transition)
    {
        var fixture = new ProviderFixture(SensitiveActionPolicy.RequireUserAction);
        await using var session = new SessionTestContext();
        await session.Store.AuthenticateAsync(fixture.Account, TestContext.Current.CancellationToken);
        await session.Store.UnlockAsync(fixture.State);
        await using var accessor = new AppSessionAccessor(session.Store);
        var provider = new SshKeyProvider(accessor, session.Operations, fixture.DialogService, fixture.SettingsStore);
        var dialogStarted = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.DialogService.ShowAsync<SshUserActionResponse, SshUserActionRequest>(
                Arg.Any<SshUserActionRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                CancellationToken token = call.Arg<CancellationToken>();
                dialogStarted.SetResult(token);
                return WaitForDialogCancellation(token);
            });
        Task<SshAgentSignature?> signing = provider.GetSignatureAsync(fixture.CreateRequest(), null, false, CancellationToken.None).AsTask();
        CancellationToken dialogToken = await dialogStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        if (transition == "lock")
        {
            await session.Store.LockAsync(TestContext.Current.CancellationToken);
        }
        else if (transition == "signout")
        {
            await session.Store.SignOutAsync(TestContext.Current.CancellationToken);
        }
        else if (transition == "switch")
        {
            await session.Store.LockAsync(TestContext.Current.CancellationToken);
            await session.Store.SignOutAsync(TestContext.Current.CancellationToken);
            var secondAccount = AccountTestData.Profile(AccountTestData.SecondUserId, "second@example.test", "second");
            await session.Store.AuthenticateAsync(secondAccount, TestContext.Current.CancellationToken);
            await session.Store.UnlockAsync(CreateAccessor(secondAccount, fixture.Key.CreateCipher()).State);
        }
        else if (transition == "reunlock")
        {
            await session.Store.LockAsync(TestContext.Current.CancellationToken);
            await session.Store.UnlockAsync(CreateAccessor(fixture.Account, fixture.Key.CreateCipher()).State);
        }

        Assert.Same(fixture.State, accessor.State);
        Assert.True(dialogToken.IsCancellationRequested);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => signing);
    }

    private async Task<UserDialogResult<SshUserActionResponse>> WaitForDialogCancellation(CancellationToken token)
    {
        await Task.Delay(Timeout.Infinite, token);
        return UserDialogResult<SshUserActionResponse>.Cancelled;
    }

    [Fact]
    public async Task GetSignature_returns_no_signature_if_the_approved_key_was_replaced()
    {
        var fixture = new ProviderFixture(SensitiveActionPolicy.RequireUserAction);
        fixture.ConfigureVault(fixture.Key.CreateCipher());
        IUnlockedVault vault = fixture.Vault;
        fixture.DialogService.ShowAsync<SshUserActionResponse, SshUserActionRequest>(
                Arg.Any<SshUserActionRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                vault.GetCiphers(Arg.Any<VaultCipherQuery>()).Returns(Array.Empty<Cipher>());
                return Task.FromResult(UserDialogResult<SshUserActionResponse>.Completed(new SshUserActionResponse(true)));
            });

        SshAgentSignature? signature = await fixture.Provider.GetSignatureAsync(
            fixture.CreateRequest(),
            null,
            false,
            CancellationToken.None);

        Assert.Null(signature);
    }

    private sealed class ProviderFixture
    {
        public ProviderFixture(SensitiveActionPolicy policy)
        {
            Account = AccountTestData.Profile(
                AccountTestData.FirstUserId,
                "user@example.test",
                "first");
            (Accessor, State) = CreateAccessor(Account, Key.CreateCipher());
            Operations.WaitUntilUnlockedAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
            SettingsStore.Get(AppSettingKeys.SshAgent.UserVerificationPolicyKey).Returns(policy);
            DialogService.ShowAsync<SshUserActionResponse, SshUserActionRequest>(
                    Arg.Any<SshUserActionRequest>(),
                    Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(UserDialogResult<SshUserActionResponse>.Completed(new SshUserActionResponse(true))));
            Provider = new SshKeyProvider(Accessor, Operations, DialogService, SettingsStore);
        }

        public SyntheticOpenSshKey Key { get; } = SshAgentTestData.CreateKey();
        public AccountProfile Account { get; }
        public IAppSessionAccessor Accessor { get; }
        public AppSessionState.Unlocked State { get; }
        public IUnlockedVault Vault => State.Vault;
        public IAppSessionOperations Operations { get; } = Substitute.For<IAppSessionOperations>();
        public IUserDialogService DialogService { get; } = Substitute.For<IUserDialogService>();
        public ISettingsStore SettingsStore { get; } = Substitute.For<ISettingsStore>();
        public SshKeyProvider Provider { get; }

        public SshAgentSignRequest CreateRequest(byte[]? data = null) =>
            new(Key.PublicKeyBlob, data ?? [0x01, 0x02], SshAgentSignatureFlags.None);

        public void ConfigureVault(params Cipher[] ciphers)
        {
            Vault.GetCiphers(Arg.Any<VaultCipherQuery>()).Returns(ciphers);
        }

    }
}

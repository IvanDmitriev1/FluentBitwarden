using System.Buffers.Binary;
using System.Security.Cryptography;
using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;
using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.Contracts.AppSession.State;
using FluentBitwarden.Contracts.AppSession.Unlock;
using FluentBitwarden.Contracts.Integrations.Passkey;
using FluentBitwarden.Contracts.Modules.Passkey.Models;

namespace FluentBitwarden.AppHost.IntegrationTests.Ipc;

public sealed class PasskeyIpcHandlerTests
{
    [Fact]
    public async Task SelectCredentialAsync_maps_selected_credential_and_signs_the_request_binding()
    {
        const string rpId = "login.example.test";
        const string userName = "ada@example.test";
        const string displayName = "Ada Lovelace";
        byte[] credentialId = [0x11, 0x22, 0x33];
        byte[] userHandle = [0xA1, 0xB2, 0xC3];
        byte[] rpIdHash = Enumerable.Range(1, 32).Select(static value => (byte)value).ToArray();
        byte[] clientDataHash = Enumerable.Range(33, 32).Select(static value => (byte)value).ToArray();
        const uint counter = 0x10203040;
        using ECDsa signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        byte[] privateKey = signingKey.ExportPkcs8PrivateKey();
        try
        {
            var credential = new Fido2Credential
            {
                CredentialId = credentialId,
                KeyType = Fido2CredentialKeyType.PublicKey,
                KeyAlgorithm = Fido2CredentialKeyAlgorithm.Ecdsa,
                KeyCurve = Fido2CredentialKeyCurve.P256,
                KeyValue = privateKey,
                RpId = rpId,
                RpName = "Example",
                UserHandle = userHandle,
                UserName = userName,
                UserDisplayName = displayName,
                Counter = counter,
                Discoverable = true,
                CreationDate = new DateTimeOffset(2025, 1, 2, 3, 4, 5, TimeSpan.Zero)
            };
            IPasskeyDialogClient dialog = Substitute.For<IPasskeyDialogClient>();
            dialog.ShowPasskeySelectionDialogAsync(
                    Arg.Any<PasskeySelectCredentialRequest>(),
                    Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(credential));
            await using var context = new PasskeyIpcHandlerTestContext(dialog);
            var account = AccountTestData.Profile(AccountTestData.FirstUserId, "ada@example.test", "passkey");
            Assert.IsType<SessionUnlockOutcome.Success>(
                await context.UnlockAsync(account, TestContext.Current.CancellationToken));
            var request = new PasskeyGetAssertionRequest(rpId, rpIdHash, clientDataHash);
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;

            PasskeyAssertionResponse response = await context.Handler.SelectCredentialAsync(request, cancellationToken);

            Assert.Equal(credentialId, response.CredentialId);
            Assert.Equal(userHandle, response.UserId);
            Assert.Equal(userName, response.UserName);
            Assert.Equal(displayName, response.UserDisplayName);
            Assert.Equal(37, response.AuthenticatorData.Length);
            Assert.Equal(rpIdHash, response.AuthenticatorData.AsSpan(0, 32).ToArray());
            Assert.Equal((byte)0x1D, response.AuthenticatorData[32]);
            Assert.Equal(counter, BinaryPrimitives.ReadUInt32BigEndian(response.AuthenticatorData.AsSpan(33)));

            byte[] signedData = new byte[response.AuthenticatorData.Length + clientDataHash.Length];
            response.AuthenticatorData.CopyTo(signedData, 0);
            clientDataHash.CopyTo(signedData, response.AuthenticatorData.Length);
            byte[] signedHash = SHA256.HashData(signedData);
            Assert.True(signingKey.VerifyHash(
                signedHash,
                response.Signature,
                DSASignatureFormat.Rfc3279DerSequence));

            clientDataHash[0] ^= 0x80;
            response.AuthenticatorData.CopyTo(signedData, 0);
            clientDataHash.CopyTo(signedData, response.AuthenticatorData.Length);
            Assert.False(signingKey.VerifyHash(
                SHA256.HashData(signedData),
                response.Signature,
                DSASignatureFormat.Rfc3279DerSequence));

            await dialog.Received(1).ShowPasskeySelectionDialogAsync(
                new PasskeySelectCredentialRequest(rpId),
                cancellationToken);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(privateKey);
        }
    }

    [Fact]
    public async Task SelectCredentialAsync_propagates_dialog_cancellation()
    {
        var cancellation = new OperationCanceledException("Synthetic passkey selection cancellation.");
        IPasskeyDialogClient dialog = Substitute.For<IPasskeyDialogClient>();
        dialog.ShowPasskeySelectionDialogAsync(
                Arg.Any<PasskeySelectCredentialRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Fido2Credential>(cancellation));
        await using var context = new PasskeyIpcHandlerTestContext(dialog);
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "passkey");
        Assert.IsType<SessionUnlockOutcome.Success>(
            await context.UnlockAsync(account, TestContext.Current.CancellationToken));
        var request = new PasskeyGetAssertionRequest("login.example.test", new byte[32], new byte[32]);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        OperationCanceledException exception = await Assert.ThrowsAsync<OperationCanceledException>(
            () => context.Handler.SelectCredentialAsync(request, cancellationToken));

        Assert.Same(cancellation, exception);
        _ = dialog.Received(1).ShowPasskeySelectionDialogAsync(
            new PasskeySelectCredentialRequest(request.RpId),
            cancellationToken);
    }

    [Fact]
    public async Task SelectCredentialAsync_propagates_dialog_failure()
    {
        var failure = new InvalidOperationException("Synthetic passkey dialog failure.");
        IPasskeyDialogClient dialog = Substitute.For<IPasskeyDialogClient>();
        dialog.ShowPasskeySelectionDialogAsync(
                Arg.Any<PasskeySelectCredentialRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Fido2Credential>(failure));
        await using var context = new PasskeyIpcHandlerTestContext(dialog);
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "passkey");
        Assert.IsType<SessionUnlockOutcome.Success>(
            await context.UnlockAsync(account, TestContext.Current.CancellationToken));
        var request = new PasskeyGetAssertionRequest("login.example.test", new byte[32], new byte[32]);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Handler.SelectCredentialAsync(request, cancellationToken));

        Assert.Same(failure, exception);
        _ = dialog.Received(1).ShowPasskeySelectionDialogAsync(
            new PasskeySelectCredentialRequest(request.RpId),
            cancellationToken);
    }

    [Fact]
    public async Task SelectCredentialAsync_rejects_not_authenticated_session_before_showing_selection()
    {
        const string rpId = "login.example.test";
        IPasskeyDialogClient dialog = Substitute.For<IPasskeyDialogClient>();
        await using var context = new PasskeyIpcHandlerTestContext(dialog);
        Fido2Credential credential = context.CreateCredential(rpId);
        dialog.ShowPasskeySelectionDialogAsync(
                Arg.Any<PasskeySelectCredentialRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(credential));
        Assert.IsType<AppSessionState.NotAuthenticated>(context.Session.Service.State);
        var request = new PasskeyGetAssertionRequest(rpId, new byte[32], new byte[32]);

        await context.WaitForCompletionAsync(
            Assert.ThrowsAsync<UnlockedSessionRequiredException>(
                () => context.Handler.SelectCredentialAsync(request, CancellationToken.None)));

        _ = dialog.DidNotReceive().ShowPasskeySelectionDialogAsync(
            Arg.Any<PasskeySelectCredentialRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SelectCredentialAsync_rejects_locked_session_before_showing_selection()
    {
        const string rpId = "login.example.test";
        IPasskeyDialogClient dialog = Substitute.For<IPasskeyDialogClient>();
        await using var context = new PasskeyIpcHandlerTestContext(dialog);
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "passkey");
        Assert.IsType<SessionUnlockOutcome.Success>(
            await context.UnlockAsync(account, TestContext.Current.CancellationToken));
        await context.Session.Service.LockAsync(TestContext.Current.CancellationToken);
        Assert.IsType<AppSessionState.Locked>(context.Session.Service.State);
        Fido2Credential credential = context.CreateCredential(rpId);
        dialog.ShowPasskeySelectionDialogAsync(
                Arg.Any<PasskeySelectCredentialRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(credential));
        var request = new PasskeyGetAssertionRequest(rpId, new byte[32], new byte[32]);

        await context.WaitForCompletionAsync(
            Assert.ThrowsAsync<UnlockedSessionRequiredException>(
                () => context.Handler.SelectCredentialAsync(request, CancellationToken.None)));

        _ = dialog.DidNotReceive().ShowPasskeySelectionDialogAsync(
            Arg.Any<PasskeySelectCredentialRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SelectCredentialAsync_cancels_when_session_locks_during_selection()
    {
        const string rpId = "login.example.test";
        IPasskeyDialogClient dialog = Substitute.For<IPasskeyDialogClient>();
        await using var context = new PasskeyIpcHandlerTestContext(dialog);
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "passkey");
        Assert.IsType<SessionUnlockOutcome.Success>(
            await context.UnlockAsync(account, TestContext.Current.CancellationToken));
        var dialogEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var selectedCredential = new TaskCompletionSource<Fido2Credential>(TaskCreationOptions.RunContinuationsAsynchronously);
        dialog.ShowPasskeySelectionDialogAsync(
                Arg.Any<PasskeySelectCredentialRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                dialogEntered.TrySetResult();
                return selectedCredential.Task;
            });
        var request = new PasskeyGetAssertionRequest(rpId, new byte[32], new byte[32]);
        Task<PasskeyAssertionResponse> operation = context.Handler.SelectCredentialAsync(request, CancellationToken.None);

        await context.WaitForCompletionAsync(dialogEntered.Task);
        await context.Session.Service.LockAsync(TestContext.Current.CancellationToken);
        selectedCredential.SetResult(context.CreateCredential(rpId));
        await context.WaitForCompletionAsync(
            Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation));
    }

    [Fact]
    public async Task SelectCredentialAsync_cancels_when_account_changes_during_selection()
    {
        const string rpId = "login.example.test";
        IPasskeyDialogClient dialog = Substitute.For<IPasskeyDialogClient>();
        await using var context = new PasskeyIpcHandlerTestContext(dialog);
        var firstAccount = AccountTestData.Profile(AccountTestData.FirstUserId, "first@example.test", "first");
        var secondAccount = AccountTestData.Profile(AccountTestData.SecondUserId, "second@example.test", "second");
        Assert.IsType<SessionUnlockOutcome.Success>(
            await context.UnlockAsync(firstAccount, TestContext.Current.CancellationToken));
        var dialogEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var selectedCredential = new TaskCompletionSource<Fido2Credential>(TaskCreationOptions.RunContinuationsAsynchronously);
        dialog.ShowPasskeySelectionDialogAsync(
                Arg.Any<PasskeySelectCredentialRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                dialogEntered.TrySetResult();
                return selectedCredential.Task;
            });
        var request = new PasskeyGetAssertionRequest(rpId, new byte[32], new byte[32]);
        Task<PasskeyAssertionResponse> operation = context.Handler.SelectCredentialAsync(request, CancellationToken.None);

        await context.WaitForCompletionAsync(dialogEntered.Task);
        await context.Session.Service.LockAsync(TestContext.Current.CancellationToken);
        Assert.IsType<SessionUnlockOutcome.Success>(
            await context.UnlockAsync(secondAccount, TestContext.Current.CancellationToken));
        selectedCredential.SetResult(context.CreateCredential(rpId));
        await context.WaitForCompletionAsync(
            Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation));
    }

    [Fact]
    public async Task SelectCredentialAsync_cancels_when_same_account_is_reunlocked_during_selection()
    {
        const string rpId = "login.example.test";
        IPasskeyDialogClient dialog = Substitute.For<IPasskeyDialogClient>();
        await using var context = new PasskeyIpcHandlerTestContext(dialog);
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "passkey");
        Assert.IsType<SessionUnlockOutcome.Success>(
            await context.UnlockAsync(account, TestContext.Current.CancellationToken));
        var dialogEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var selectedCredential = new TaskCompletionSource<Fido2Credential>(TaskCreationOptions.RunContinuationsAsynchronously);
        dialog.ShowPasskeySelectionDialogAsync(
                Arg.Any<PasskeySelectCredentialRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                dialogEntered.TrySetResult();
                return selectedCredential.Task;
            });
        var request = new PasskeyGetAssertionRequest(rpId, new byte[32], new byte[32]);
        Task<PasskeyAssertionResponse> operation = context.Handler.SelectCredentialAsync(request, CancellationToken.None);

        await context.WaitForCompletionAsync(dialogEntered.Task);
        await context.Session.Service.LockAsync(TestContext.Current.CancellationToken);
        Assert.IsType<SessionUnlockOutcome.Success>(
            await context.UnlockAsync(account, TestContext.Current.CancellationToken));
        selectedCredential.SetResult(context.CreateCredential(rpId));
        await context.WaitForCompletionAsync(
            Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation));
    }
}

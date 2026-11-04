using System.Buffers.Binary;
using System.Security.Cryptography;
using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;
using FluentBitwarden.Contracts.AppSession.Unlock;
using FluentBitwarden.Contracts.Modules.Passkey.Models;

namespace FluentBitwarden.AppHost.IntegrationTests.Ipc;

public sealed class PasskeyIpcHandlerTests
{
    [Fact]
    public async Task SelectCredentialAsync_waits_for_unlock_before_showing_dialog()
    {
        FluentBitwarden.Contracts.Infrastructure.UserDialog.IUserDialogService dialog =
            Substitute.For<FluentBitwarden.Contracts.Infrastructure.UserDialog.IUserDialogService>();
        await using var context = new PasskeyIpcHandlerTestContext(dialog);
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "ada@example.test", "passkey");
        await context.Session.Store.AuthenticateAsync(account, TestContext.Current.CancellationToken);
        var request = new PasskeyGetAssertionRequest("login.example.test", new byte[32], new byte[32]);
        using var cancellation = new CancellationTokenSource();
        var dialogRequest = new TaskCompletionSource<PasskeySelectCredentialRequest>(TaskCreationOptions.RunContinuationsAsynchronously);
        dialog.ShowAsync<Fido2Credential, PasskeySelectCredentialRequest>(
                Arg.Any<PasskeySelectCredentialRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                dialogRequest.TrySetResult(call.Arg<PasskeySelectCredentialRequest>());
                return Task.FromResult(FluentBitwarden.Contracts.Infrastructure.UserDialog.UserDialogResult<Fido2Credential>.Completed(context.CreateCredential(request.RpId)));
            });

        Task<PasskeyAssertionResponse> selection = context.Handler.SelectCredentialAsync(request, cancellation.Token);
        Assert.False(selection.IsCompleted);
        Assert.False(dialogRequest.Task.IsCompleted);

        Assert.IsType<SessionUnlockOutcome.Success>(await context.UnlockAsync(account, TestContext.Current.CancellationToken));
        await context.WaitForCompletionAsync(selection);

        Assert.Equal(new PasskeySelectCredentialRequest(request.RpId), await dialogRequest.Task);
        _ = await dialog.Received(1).ShowAsync<Fido2Credential, PasskeySelectCredentialRequest>(
            new PasskeySelectCredentialRequest(request.RpId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SelectCredentialAsync_cancellation_while_waiting_prevents_dialog()
    {
        FluentBitwarden.Contracts.Infrastructure.UserDialog.IUserDialogService dialog =
            Substitute.For<FluentBitwarden.Contracts.Infrastructure.UserDialog.IUserDialogService>();
        await using var context = new PasskeyIpcHandlerTestContext(dialog);
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "ada@example.test", "passkey");
        await context.Session.Store.AuthenticateAsync(account, CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        var request = new PasskeyGetAssertionRequest("login.example.test", new byte[32], new byte[32]);
        var dialogCalled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        dialog.ShowAsync<Fido2Credential, PasskeySelectCredentialRequest>(
                Arg.Any<PasskeySelectCredentialRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                dialogCalled.TrySetResult();
                return Task.FromResult(FluentBitwarden.Contracts.Infrastructure.UserDialog.UserDialogResult<Fido2Credential>.Cancelled);
            });

        Task<PasskeyAssertionResponse> selection = context.Handler.SelectCredentialAsync(request, cancellation.Token);
        Assert.False(selection.IsCompleted);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => selection);
        Assert.False(dialogCalled.Task.IsCompleted);
    }

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
            FluentBitwarden.Contracts.Infrastructure.UserDialog.IUserDialogService dialog =
                Substitute.For<FluentBitwarden.Contracts.Infrastructure.UserDialog.IUserDialogService>();
            dialog.ShowAsync<Fido2Credential, PasskeySelectCredentialRequest>(
                    Arg.Any<PasskeySelectCredentialRequest>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(FluentBitwarden.Contracts.Infrastructure.UserDialog.UserDialogResult<Fido2Credential>.Completed(credential)));
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

            await dialog.Received(1).ShowAsync<Fido2Credential, PasskeySelectCredentialRequest>(
                new PasskeySelectCredentialRequest(rpId),
                Arg.Any<CancellationToken>());
        }
        finally
        {
            CryptographicOperations.ZeroMemory(privateKey);
        }
    }

    [Fact]
    public async Task SelectCredentialAsync_propagates_dialog_cancellation()
    {
        FluentBitwarden.Contracts.Infrastructure.UserDialog.IUserDialogService dialog =
            Substitute.For<FluentBitwarden.Contracts.Infrastructure.UserDialog.IUserDialogService>();
        dialog.ShowAsync<Fido2Credential, PasskeySelectCredentialRequest>(
                Arg.Any<PasskeySelectCredentialRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(FluentBitwarden.Contracts.Infrastructure.UserDialog.UserDialogResult<Fido2Credential>.Cancelled));
        await using var context = new PasskeyIpcHandlerTestContext(dialog);
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "passkey");
        Assert.IsType<SessionUnlockOutcome.Success>(
            await context.UnlockAsync(account, TestContext.Current.CancellationToken));
        var request = new PasskeyGetAssertionRequest("login.example.test", new byte[32], new byte[32]);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        OperationCanceledException exception = await Assert.ThrowsAsync<OperationCanceledException>(
            () => context.Handler.SelectCredentialAsync(request, cancellationToken));

        Assert.IsType<OperationCanceledException>(exception);
        _ = dialog.Received(1).ShowAsync<Fido2Credential, PasskeySelectCredentialRequest>(
            new PasskeySelectCredentialRequest(request.RpId),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("lock")]
    [InlineData("signout")]
    [InlineData("switch")]
    [InlineData("reunlock")]
    public async Task SelectCredentialAsync_cancels_pending_selection_when_session_retires_captured_state(string transition)
    {
        FluentBitwarden.Contracts.Infrastructure.UserDialog.IUserDialogService dialog =
            Substitute.For<FluentBitwarden.Contracts.Infrastructure.UserDialog.IUserDialogService>();
        await using var context = new PasskeyIpcHandlerTestContext(dialog);
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "ada@example.test", "passkey");
        var secondAccount = AccountTestData.Profile(AccountTestData.SecondUserId, "grace@example.test", "second");
        Assert.IsType<SessionUnlockOutcome.Success>(await context.UnlockAsync(account, TestContext.Current.CancellationToken));
        var request = new PasskeyGetAssertionRequest("login.example.test", new byte[32], new byte[32]);
        var dialogStarted = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        dialog.ShowAsync<Fido2Credential, PasskeySelectCredentialRequest>(
                Arg.Any<PasskeySelectCredentialRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                CancellationToken token = call.Arg<CancellationToken>();
                dialogStarted.SetResult(token);
                return WaitForDialogCancellation(token);
            });
        Task<PasskeyAssertionResponse> selection = context.Handler.SelectCredentialAsync(request, CancellationToken.None);
        CancellationToken dialogToken = await dialogStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        await ApplyTransitionAsync(context, transition, account, secondAccount);

        Assert.True(dialogToken.IsCancellationRequested);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => selection.WaitAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SelectCredentialAsync_rejects_credential_returned_after_session_revocation()
    {
        FluentBitwarden.Contracts.Infrastructure.UserDialog.IUserDialogService dialog =
            Substitute.For<FluentBitwarden.Contracts.Infrastructure.UserDialog.IUserDialogService>();
        await using var context = new PasskeyIpcHandlerTestContext(dialog);
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "ada@example.test", "passkey");
        Assert.IsType<SessionUnlockOutcome.Success>(await context.UnlockAsync(account, TestContext.Current.CancellationToken));
        var request = new PasskeyGetAssertionRequest("login.example.test", new byte[32], new byte[32]);
        dialog.ShowAsync<Fido2Credential, PasskeySelectCredentialRequest>(
                Arg.Any<PasskeySelectCredentialRequest>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                await context.Session.Operations.LockAsync(TestContext.Current.CancellationToken);
                return FluentBitwarden.Contracts.Infrastructure.UserDialog.UserDialogResult<Fido2Credential>.Completed(
                    context.CreateCredential(request.RpId));
            });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.Handler.SelectCredentialAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task SelectCredentialAsync_rejects_credential_returned_after_request_cancellation()
    {
        FluentBitwarden.Contracts.Infrastructure.UserDialog.IUserDialogService dialog =
            Substitute.For<FluentBitwarden.Contracts.Infrastructure.UserDialog.IUserDialogService>();
        await using var context = new PasskeyIpcHandlerTestContext(dialog);
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "ada@example.test", "passkey");
        Assert.IsType<SessionUnlockOutcome.Success>(await context.UnlockAsync(account, TestContext.Current.CancellationToken));
        var request = new PasskeyGetAssertionRequest("login.example.test", new byte[32], new byte[32]);
        using var cancellation = new CancellationTokenSource();
        dialog.ShowAsync<Fido2Credential, PasskeySelectCredentialRequest>(
                Arg.Any<PasskeySelectCredentialRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                Assert.NotEqual(cancellation.Token, call.Arg<CancellationToken>());
                cancellation.Cancel();
                return Task.FromResult(FluentBitwarden.Contracts.Infrastructure.UserDialog.UserDialogResult<Fido2Credential>.Completed(
                    context.CreateCredential(request.RpId)));
            });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => context.Handler.SelectCredentialAsync(request, cancellation.Token));
    }

    private static async Task<FluentBitwarden.Contracts.Infrastructure.UserDialog.UserDialogResult<Fido2Credential>> WaitForDialogCancellation(CancellationToken token)
    {
        await Task.Delay(Timeout.Infinite, token);
        return FluentBitwarden.Contracts.Infrastructure.UserDialog.UserDialogResult<Fido2Credential>.Cancelled;
    }

    private static async Task ApplyTransitionAsync(
        PasskeyIpcHandlerTestContext context,
        string transition,
        AccountProfile account,
        AccountProfile secondAccount)
    {
        if (transition == "signout")
        {
            await context.Session.Store.SignOutAsync(TestContext.Current.CancellationToken);
            return;
        }

        await context.Session.Operations.LockAsync(TestContext.Current.CancellationToken);
        if (transition == "switch")
        {
            Assert.IsType<SessionUnlockOutcome.Success>(await context.UnlockAsync(secondAccount, TestContext.Current.CancellationToken));
        }
        else if (transition == "reunlock")
        {
            Assert.IsType<SessionUnlockOutcome.Success>(await context.UnlockAsync(account, TestContext.Current.CancellationToken));
        }
    }

    [Fact]
    public async Task SelectCredentialAsync_propagates_dialog_failure()
    {
        var failure = new InvalidOperationException("Synthetic passkey dialog failure.");
        FluentBitwarden.Contracts.Infrastructure.UserDialog.IUserDialogService dialog =
            Substitute.For<FluentBitwarden.Contracts.Infrastructure.UserDialog.IUserDialogService>();
        dialog.ShowAsync<Fido2Credential, PasskeySelectCredentialRequest>(
                Arg.Any<PasskeySelectCredentialRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<FluentBitwarden.Contracts.Infrastructure.UserDialog.UserDialogResult<Fido2Credential>>(failure));
        await using var context = new PasskeyIpcHandlerTestContext(dialog);
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "passkey");
        Assert.IsType<SessionUnlockOutcome.Success>(
            await context.UnlockAsync(account, TestContext.Current.CancellationToken));
        var request = new PasskeyGetAssertionRequest("login.example.test", new byte[32], new byte[32]);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Handler.SelectCredentialAsync(request, cancellationToken));

        Assert.Same(failure, exception);
        _ = dialog.Received(1).ShowAsync<Fido2Credential, PasskeySelectCredentialRequest>(
            new PasskeySelectCredentialRequest(request.RpId),
            Arg.Any<CancellationToken>());
    }
}

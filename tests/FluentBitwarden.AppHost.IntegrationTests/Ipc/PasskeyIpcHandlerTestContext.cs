using System.Security.Cryptography;
using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.Ipc;
using FluentBitwarden.AppHost.IntegrationTests.AppSession;
using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.AppSession.State;
using FluentBitwarden.Contracts.AppSession.Unlock;
using FluentBitwarden.Contracts.Integrations.Passkey;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.AppHost.IntegrationTests.Ipc;

internal sealed class PasskeyIpcHandlerTestContext : IAsyncDisposable
{
    private readonly ServiceProvider _services;
    private readonly List<byte[]> _exportedSigningPrivateKeys = [];
    private int _nextAccountKeyGeneration = 1;
    private int _nextCredentialId = 1;

    public PasskeyIpcHandlerTestContext(IPasskeyDialogClient dialog)
    {
        Dialog = dialog;
        Session = new SessionTestContext();

        var services = new ServiceCollection();
        services.AddSingleton<IAppSessionService>(Session.Service);
        services.AddSingleton<IPasskeyDialogClient>(Dialog);
        services.AddTransient<PasskeyIpcHandler>();
        _services = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });

        Handler = _services.GetRequiredService<PasskeyIpcHandler>();
    }

    public IPasskeyDialogClient Dialog { get; }
    public SessionTestContext Session { get; }
    public PasskeyIpcHandler Handler { get; }

    public Fido2Credential CreateCredential(string rpId)
    {
        using ECDsa signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        byte[] privateKey = signingKey.ExportPkcs8PrivateKey();
        _exportedSigningPrivateKeys.Add(privateKey);

        byte credentialId = (byte)_nextCredentialId++;
        return new Fido2Credential
        {
            CredentialId = [credentialId],
            KeyType = Fido2CredentialKeyType.PublicKey,
            KeyAlgorithm = Fido2CredentialKeyAlgorithm.Ecdsa,
            KeyCurve = Fido2CredentialKeyCurve.P256,
            KeyValue = privateKey,
            RpId = rpId,
            RpName = "Synthetic passkey test",
            UserHandle = [0xA1, credentialId],
            UserName = "synthetic@example.test",
            UserDisplayName = "Synthetic User",
            Counter = (uint)credentialId,
            Discoverable = true,
            CreationDate = new DateTimeOffset(2025, 1, 2, 3, 4, 5, TimeSpan.Zero)
        };
    }

    public async Task<SessionUnlockOutcome> UnlockAsync(AccountProfile account, CancellationToken cancellationToken)
    {
        if (Session.Service.State is AppSessionState.Unlocked)
        {
            throw new InvalidOperationException("Lock the current test session before unlocking another test session.");
        }

        byte keyGeneration = (byte)_nextAccountKeyGeneration++;
        byte[] keyBytes = new byte[32];
        Array.Fill(keyBytes, keyGeneration);
        var keySession = new TestAccountKeySession(new UnlockedUserKey(account.UserId, keyBytes));
        Session.ConfigureAccount(
            account,
            new AccountKeyUnlockResult.Success(keySession),
            Substitute.For<IUnlockedVault>());

        try
        {
            SessionUnlockOutcome outcome = await Session.Service.UnlockAsync(
                new SessionUnlockRequest.MasterPasswordRequest(account.UserId, "synthetic-passkey-test"),
                cancellationToken);

            if (outcome is not SessionUnlockOutcome.Success)
            {
                keySession.Dispose();
            }

            return outcome;
        }
        catch
        {
            if (Session.Service.State is not AppSessionState.Unlocked)
            {
                keySession.Dispose();
            }

            throw;
        }
    }

    public async Task WaitForCompletionAsync(Task task)
    {
        await task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (Session.Service.State is AppSessionState.Unlocked)
            {
                await Session.Service.LockAsync(CancellationToken.None);
            }
        }
        finally
        {
            try
            {
                await _services.DisposeAsync();
            }
            finally
            {
                foreach (byte[] privateKey in _exportedSigningPrivateKeys)
                {
                    CryptographicOperations.ZeroMemory(privateKey);
                }
            }
        }
    }
}

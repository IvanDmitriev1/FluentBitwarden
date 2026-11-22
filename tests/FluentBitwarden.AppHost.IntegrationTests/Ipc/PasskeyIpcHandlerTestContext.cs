using System.Security.Cryptography;
using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.AppSession.Services;
using FluentBitwarden.AppHost.IntegrationTests.AppSession;
using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Search;
using FluentBitwarden.Contracts.AppSession.State;
using FluentBitwarden.Contracts.AppSession.Unlock;
using Microsoft.Extensions.DependencyInjection;
using FluentBitwarden.AppHost.Ipc;

namespace FluentBitwarden.AppHost.IntegrationTests.Ipc;

internal sealed class PasskeyIpcHandlerTestContext : IAsyncDisposable
{
    private readonly ServiceProvider _services;
    private readonly AsyncServiceScope _scope;
    private readonly List<byte[]> _exportedSigningPrivateKeys = [];
    private int _nextAccountKeyGeneration = 1;
    private int _nextCredentialId = 1;
    private LoginCipher _candidateCipher = null!;

    public PasskeyIpcHandlerTestContext(FluentBitwarden.Contracts.Infrastructure.UserDialog.IUserDialogService dialog)
    {
        Dialog = dialog;
        Session = new SessionTestContext();
        CandidateCipherId = CipherId.Parse("passkey-test-cipher");
        SetCandidateCredential(CreateCredential("login.example.test"));

        var services = new ServiceCollection();
        services.AddSingleton<IAppSessionOperations>(Session.Operations);
        services.AddSingleton(Session.Store);
        services.AddScoped<IAppSessionAccessor, AppSessionAccessor>();
        services.AddScoped<IVaultOrganizationReader>(_ => Substitute.For<IVaultOrganizationReader>());
        services.AddSingleton<FluentBitwarden.Contracts.Infrastructure.UserDialog.IUserDialogService>(Dialog);
        services.AddTransient<PasskeyIpcHandler>();
        _services = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });

        _scope = _services.CreateAsyncScope();
        Handler = _scope.ServiceProvider.GetRequiredService<PasskeyIpcHandler>();
    }

    public FluentBitwarden.Contracts.Infrastructure.UserDialog.IUserDialogService Dialog { get; }
    public SessionTestContext Session { get; }
    public PasskeyIpcHandler Handler { get; }
    public CipherId CandidateCipherId { get; }

    public void SetCandidateCredential(Fido2Credential credential) =>
        _candidateCipher = new LoginCipher
        {
            Id = CandidateCipherId,
            Name = "Synthetic passkey item",
            Favorite = false,
            Reprompt = false,
            RevisionDate = credential.CreationDate,
            CreationDate = credential.CreationDate,
            DeletedDate = null,
            Fido2Credential = credential
        };

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
        if (Session.Store.State is AppSessionStateSnapshot.Unlocked)
        {
            throw new InvalidOperationException("Lock the current test session before unlocking another test session.");
        }

        byte keyGeneration = (byte)_nextAccountKeyGeneration++;
        byte[] keyBytes = new byte[32];
        Array.Fill(keyBytes, keyGeneration);
        var keySession = new TestAccountKeySession(new UnlockedUserKey(account.UserId, keyBytes));
        var vault = Substitute.For<IUnlockedVault>();
        vault.GetCiphers(Arg.Any<VaultCipherQuery>()).Returns([_candidateCipher]);
        vault.GetCipher(Arg.Any<CipherId>()).Returns(call =>
            call.Arg<CipherId>() == CandidateCipherId ? _candidateCipher : null);
        Session.ConfigureAccount(
            account,
            new AccountKeyUnlockResult.Success(keySession),
            vault);

        try
        {
            SessionUnlockOutcome outcome = await Session.Operations.UnlockAsync(
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
            if (Session.Store.State is not AppSessionStateSnapshot.Unlocked)
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
            await Session.DisposeAsync();
        }
        finally
        {
            try
            {
                await _scope.DisposeAsync();
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
}

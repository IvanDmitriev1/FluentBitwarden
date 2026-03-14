using System.Security.Cryptography;
using BitwaredApi.Abstractions;
using BitwaredApi.Abstractions.Exceptions;
using BitwaredApi.Models.Auth;
using BitwaredApi.Models.Vault;
using BitwaredApi.Utils;

namespace BitwaredApi.Services;

internal sealed class AuthenticationWorkflow(
    IIdentityClient identityClient,
    IApiClient apiClient,
    ICryptoService cryptoService)
    : IAuthenticationWorkflow
{
    public async ValueTask<PasswordSignInOutcome> SignInWithPasswordAsync(
        PasswordSignInRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        PreloginResponseModel prelogin = await identityClient.PreloginAsync(
            request.Context.Environment,
            request.Email,
            cancellationToken);

        return await SignInWithPasswordAsync(request, prelogin, cancellationToken);
    }

    public async ValueTask<AuthenticationOutcome> ContinueTwoFactorAsync(
        TwoFactorSignInRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        PasswordTokenRequestModel tokenRequest = new(
            CryptoService.NormalizeEmail(request.Continuation.Email),
            request.Continuation.Auth.ServerAuthorizationHash,
            ClientType.Desktop,
            request.Context.DeviceInfo.DeviceType,
            request.Context.DeviceInfo.DeviceName,
            request.Context.DeviceInfo.DeviceIdentifier,
            request.Token,
            request.Provider,
            request.Remember);

        TokenExchangeOutcome exchangeOutcome = await identityClient.ExchangePasswordAsync(
            request.Context.Environment,
            tokenRequest,
            cancellationToken);

        return exchangeOutcome switch
        {
            TokenExchangeOutcome.Success success => new AuthenticationOutcome.Success(
                await CreatePasswordAuthenticationSuccessAsync(
                    request.Context,
                    request.Continuation.Email,
                    request.Continuation.Kdf,
                    request.Continuation.Auth,
                    success.Response)),
            TokenExchangeOutcome.InvalidCredentials invalidCredentials => new AuthenticationOutcome.InvalidCredentials(
                invalidCredentials.Message),
            TokenExchangeOutcome.DeviceVerificationRequired deviceVerificationRequired => new AuthenticationOutcome.DeviceVerificationRequired(
                deviceVerificationRequired.Message),
            TokenExchangeOutcome.TwoFactorRequired twoFactorRequired => new AuthenticationOutcome.InvalidCredentials(
                twoFactorRequired.Message),
            _ => throw new ServerVersionMismatchException("The token endpoint returned an unsupported authentication outcome."),
        };
    }

    public async ValueTask<DeviceLoginStartResult> StartDeviceLoginAsync(
        DeviceLoginStartRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string normalizedEmail = CryptoService.NormalizeEmail(request.Email);
        string accessCode = AuthenticationWorkflowFactory.GenerateAccessCode();

        using RSA rsa = RSA.Create(2048);
        byte[]? privateKey = rsa.ExportPkcs8PrivateKey();
        byte[] publicKey = rsa.ExportSubjectPublicKeyInfo();

        try
        {
            string publicKeyBase64 = Convert.ToBase64String(publicKey);
            string fingerprintPhrase = cryptoService.CreateFingerprintPhrase(normalizedEmail, publicKey);

            AuthRequestCreateResponse authRequest = await apiClient.CreateAuthRequestAsync(
                request.Context.Environment,
                normalizedEmail,
                request.Context.DeviceInfo.DeviceIdentifier,
                publicKeyBase64,
                AuthRequestType.AuthenticateAndUnlock,
                accessCode,
                cancellationToken);

            DeviceSignInContinuation continuation = new(
                normalizedEmail,
                authRequest.AccessCode,
                privateKey);

            privateKey = null;

            return new DeviceLoginStartResult(
                new PendingDeviceLogin(
                    authRequest.Id,
                    authRequest.AccessCode,
                    fingerprintPhrase,
                    authRequest.Expires,
                    normalizedEmail),
                continuation);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(publicKey);
            CryptographicOperations.ZeroMemory(privateKey);
        }
    }

    public async ValueTask<DeviceApprovalOutcome> PollDeviceLoginAsync(
        DeviceLoginPollRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.PendingRequest.Expires <= DateTimeOffset.UtcNow)
        {
            return new DeviceApprovalOutcome.Expired("The device login request expired before approval.");
        }

        AuthRequestPollOutcome pollOutcome = await apiClient.GetAuthResponseAsync(
            request.Context.Environment,
            request.PendingRequest.RequestId,
            request.PendingRequest.AccessCode,
            cancellationToken);

        return pollOutcome switch
        {
            AuthRequestPollOutcome.Pending => request.PendingRequest.Expires <= DateTimeOffset.UtcNow
                ? new DeviceApprovalOutcome.Expired("The device login request expired before approval.")
                : new DeviceApprovalOutcome.Pending(),
            AuthRequestPollOutcome.Expired expired => new DeviceApprovalOutcome.Expired(expired.Message),
            AuthRequestPollOutcome.Denied denied => new DeviceApprovalOutcome.Denied(denied.Message),
            AuthRequestPollOutcome.Approved approved => new DeviceApprovalOutcome.Approved(
                await CompleteApprovedDeviceLoginAsync(
                    request.Context,
                    request.PendingRequest,
                    request.Continuation,
                    approved.Approval,
                    cancellationToken)),
            _ => throw new ServerVersionMismatchException("The auth request endpoint returned an unsupported polling outcome."),
        };
    }

    private async ValueTask<PasswordSignInOutcome> SignInWithPasswordAsync(
        PasswordSignInRequest request,
        PreloginResponseModel prelogin,
        CancellationToken cancellationToken)
    {
        MasterPasswordAuth? auth = cryptoService.DeriveMasterPasswordAuth(
            request.Email,
            request.MasterPassword,
            prelogin.Kdf);

        try
        {
            PasswordTokenRequestModel tokenRequest = new(
                CryptoService.NormalizeEmail(request.Email),
                auth.ServerAuthorizationHash,
                ClientType.Desktop,
                request.Context.DeviceInfo.DeviceType,
                request.Context.DeviceInfo.DeviceName,
                request.Context.DeviceInfo.DeviceIdentifier);

            TokenExchangeOutcome exchangeOutcome = await identityClient.ExchangePasswordAsync(
                request.Context.Environment,
                tokenRequest,
                cancellationToken);

            switch (exchangeOutcome)
            {
                case TokenExchangeOutcome.Success success:
                    return new PasswordSignInOutcome.Success(
                        await CreatePasswordAuthenticationSuccessAsync(
                            request.Context,
                            request.Email,
                            prelogin.Kdf,
                            auth,
                            success.Response));

                case TokenExchangeOutcome.TwoFactorRequired twoFactorRequired:
                    MasterPasswordAuth continuationAuth = auth;
                    auth = null;

                    return new PasswordSignInOutcome.TwoFactorRequired(
                        twoFactorRequired.Challenge,
                        new PasswordSignInContinuation(
                            request.Email,
                            prelogin.Kdf,
                            continuationAuth));

                case TokenExchangeOutcome.InvalidCredentials invalidCredentials:
                    return new PasswordSignInOutcome.InvalidCredentials(
                        invalidCredentials.Message);

                case TokenExchangeOutcome.DeviceVerificationRequired deviceVerificationRequired:
                    return new PasswordSignInOutcome.DeviceVerificationRequired(
                        deviceVerificationRequired.Message);

                default:
                    throw new ServerVersionMismatchException("The token endpoint returned an unsupported authentication outcome.");
            }
        }
        finally
        {
            auth?.Dispose();
        }
    }

    private async ValueTask<AuthenticationSuccess> CompleteApprovedDeviceLoginAsync(
        BitwardenClientContext context,
        PendingDeviceLogin pendingRequest,
        DeviceSignInContinuation continuation,
        AuthRequestApproval approval,
        CancellationToken cancellationToken)
    {
        byte[]? userKey = null;

        try
        {
            using EncString encryptedUserKey = EncString.From(approval.EncryptedUserKey);
            userKey = cryptoService.DecryptRsaWrappedKey(
                encryptedUserKey,
                continuation.PrivateKeyPkcs8);

            PasswordTokenRequestModel tokenRequest = new(
                pendingRequest.Email,
                continuation.AccessCode,
                ClientType.Desktop,
                context.DeviceInfo.DeviceType,
                context.DeviceInfo.DeviceName,
                context.DeviceInfo.DeviceIdentifier,
                AuthRequestId: pendingRequest.RequestId);

            TokenExchangeOutcome exchangeOutcome = await identityClient.ExchangePasswordAsync(
                context.Environment,
                tokenRequest,
                cancellationToken);

            if (exchangeOutcome is not TokenExchangeOutcome.Success success)
            {
                throw new ServerVersionMismatchException("The approved auth request could not be exchanged for an access token.");
            }

            AuthenticationSuccess authenticationSuccess = AuthenticationWorkflowFactory.CreateAuthenticationSuccess(
                context.Environment,
                pendingRequest.Email,
                context.DeviceInfo.DeviceIdentifier,
                success.Response,
                userKey,
                null);

            userKey = null;
            return authenticationSuccess;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(userKey);
        }
    }

    private ValueTask<AuthenticationSuccess> CreatePasswordAuthenticationSuccessAsync(
        BitwardenClientContext context,
        string email,
        KdfConfigModel fallbackKdf,
        MasterPasswordAuth auth,
        TokenResponseModel response)
    {
        string encryptedUserKey = response.GetMasterPasswordEncryptedUserKey();
        byte[]? userKey = null;

        try
        {
            using var wrappedUserKey = EncString.From(encryptedUserKey);
            userKey = cryptoService.DecryptUserKey(
                wrappedUserKey,
                auth.StretchedMasterKey);

            AuthenticationSuccess success = AuthenticationWorkflowFactory.CreateAuthenticationSuccess(
                context.Environment,
                email,
                context.DeviceInfo.DeviceIdentifier,
                response,
                userKey,
                fallbackKdf);

            userKey = null;
            return ValueTask.FromResult(success);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(userKey);
        }
    }

}

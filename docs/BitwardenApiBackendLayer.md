# `BitwardenApi` Backend Communication Layer

`BitwardenApi` is a separate project from the existing `BitwaredApi` and is intentionally scoped to backend communication only.

## Scope

- Identity token flows via `POST /connect/token`:
  - password
  - password + 2FA
  - refresh token
  - device login
  - client credentials
  - authorization code
- Vault calls:
  - `GET /sync`
  - `GET /ciphers/{id}`
  - `GET /ciphers`
  - `POST /ciphers`
  - `PUT /ciphers/{id}`
  - `DELETE /ciphers/{id}`
- Attachment calls:
  - start upload v2
  - renew upload
  - multipart upload
  - token-based download
- Notifications via SignalR on `/notifications/hub`.

## Design Constraints

- Concrete clients with direct `HttpClient` usage.
- Streaming-first responses for vault and attachment downloads.
- No crypto/decryption implementation in this project.
- No large encrypted vault DTO object graph.

## Main Types

- Shared context:
  - `BitwardenApi.Context.BitwardenClientContext`
  - `BitwardenApi.Context.BitwardenEnvironment`
  - `BitwardenApi.Context.DeviceInfo`
- `IIdentityApiClient` / `IdentityApiClient`
- `IVaultApiClient` / `VaultApiClient`
- `IAttachmentsApiClient` / `AttachmentsApiClient`
- `INotificationsClient` / `NotificationsClient`
- `ApiStreamResponse`
- `BitwardenApiException`

DI entrypoint:

- `BitwardenApi.BitwardenApiServiceCollectionExtensions.AddBitwardenApi()`

## Request DTO style

All service methods use request DTOs that include `BitwardenApi.Context.BitwardenClientContext`.

The `Identity` feature is grouped into:

- `IdentityApiClient` / `IIdentityApiClient`
- `IdentityRequests.cs`
- `IdentityModels.cs`
- `Internal/TokenRequestFormFactory.cs`

Request DTOs:

- Identity flows:
  - `PasswordLoginRequest`
  - `PasswordTwoFactorLoginRequest`
  - `RefreshLoginRequest`
  - `DeviceLoginRequest`
  - `ClientCredentialsLoginRequest`
  - `AuthorizationCodeLoginRequest`
- Vault flows:
  - `GetSyncRequest`, `GetCipherRequest`, `GetAllCiphersRequest`
  - `CreateCipherRequest`, `UpdateCipherRequest`, `DeleteCipherRequest`
- Attachments flows:
  - `StartUploadV2Request`, `RenewUploadRequest`
  - `UploadMultipartRequest`, `DownloadByTokenRequest`
- Notifications:
  - `ConnectNotificationsRequest`

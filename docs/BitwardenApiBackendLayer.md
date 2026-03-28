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
- Shared infrastructure stays under `BitwardenApi.Shared.*`.

## Project Structure

`BitwardenApi` stays a single project and single package. Each module now follows the same three-folder structure:

- `Modules/<Module>/Abstractions/`
  - public interfaces
- `Modules/<Module>/Models/`
  - request DTOs, response DTOs, strongly typed IDs, enums, and payload records
- `Modules/<Module>/Services/`
  - concrete API clients and module-local helpers

Module namespaces follow the folder layout:

- `BitwardenApi.Modules.Identity.Abstractions`
- `BitwardenApi.Modules.Identity.Models`
- `BitwardenApi.Modules.Identity.Services`
- `BitwardenApi.Modules.Vault.Abstractions`
- `BitwardenApi.Modules.Vault.Models`
- `BitwardenApi.Modules.Vault.Services`
- `BitwardenApi.Modules.Attachments.Abstractions`
- `BitwardenApi.Modules.Attachments.Models`
- `BitwardenApi.Modules.Attachments.Services`
- `BitwardenApi.Modules.Notifications.Abstractions`
- `BitwardenApi.Modules.Notifications.Models`
- `BitwardenApi.Modules.Notifications.Services`

Shared namespaces remain:

- `BitwardenApi.Shared.Context.*`
- `BitwardenApi.Shared.Cryptography.*`
- `BitwardenApi.Shared.Exceptions.*`
- `BitwardenApi.Shared.Transport.*`
- `BitwardenApi.Shared.Serialization.*`

## Main Types

- Shared context:
  - `BitwardenApi.Shared.Context.BitwardenClientContext`
  - `BitwardenApi.Shared.Context.BitwardenEnvironment`
  - `BitwardenApi.Shared.Context.DeviceInfo`
- `BitwardenApi.Modules.Identity.Abstractions.IIdentityApiClient`
- `BitwardenApi.Modules.Identity.Services.IdentityApiClient`
- `BitwardenApi.Modules.Vault.Abstractions.IVaultApiClient`
- `BitwardenApi.Modules.Vault.Services.VaultApiClient`
- `BitwardenApi.Modules.Attachments.Abstractions.IAttachmentsApiClient`
- `BitwardenApi.Modules.Attachments.Services.AttachmentsApiClient`
- `BitwardenApi.Modules.Notifications.Abstractions.INotificationsClient`
- `BitwardenApi.Modules.Notifications.Services.NotificationsClient`
- `BitwardenApi.Shared.Transport.ApiStreamResponse`
- `BitwardenApi.Shared.Exceptions.BitwardenApiException`

DI entrypoint:

- `BitwardenApi.BitwardenApiServiceCollectionExtensions.AddBitwardenApi()`
  - registers the module services directly from the project root

## Request DTO style

All service methods use request DTOs that include `BitwardenApi.Shared.Context.BitwardenClientContext`.

The `Identity` module is grouped into:

- `Abstractions/IIdentityApiClient.cs`
- `Models/IdentityRequests.cs`
- `Models/IdentityModels.cs`
- `Services/IdentityApiClient.cs`
- `Services/Internal/TokenRequestFormFactory.cs`

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

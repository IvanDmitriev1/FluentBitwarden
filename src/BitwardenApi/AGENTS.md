# BitwardenApi

## Scope and role

Root instructions apply. This bottom-layer project communicates with Bitwarden servers and represents the crypto primitives in those payloads. It has no dependency on any FluentBitwarden project.

## Local map and HTTP rules

Client/ owns account/client contexts, environment, device information, exceptions, and HTTP infrastructure. Identity/, Vault/, and Notifications/ own service APIs; Cryptography/ owns key derivation, keys, and encryption; Primitives/ and Primitives/Ids/ own shared values and IDs; Serialization/ and Encoding/ own shared codecs. API methods are grouped under Identity, Vault/Sync, Vault/Ciphers, Vault/Attachments, and Notifications. Public request/result payloads live in Identity/Models and the corresponding Vault or Notifications Models, Requests, and Responses folders; private wire DTOs and JSON contexts live under Identity/Internal, Vault/Internal, and Notifications/Internal. HTTP APIs expose an IXxxApi and an internal sealed implementation using a named HttpClient from [BitwardenApiServiceCollectionExtensions.cs](BitwardenApiServiceCollectionExtensions.cs); Notifications uses SignalR.

Authenticated vault operations take BitwardenAccountContext first and an optional cancellation token last. IServerNotificationsDispatcherFactory.CreateDispatcher creates and starts an account-scoped notifications dispatcher; its DisposeAsync cancels and awaits startup/retry before closing the connection. Identity APIs take request objects carrying BitwardenClientContext; only IWebAuthnIdentityApi.GetAuthenticationAssertionOptionsAsync takes BitwardenClientContext directly. Attach account context with SetBitwardenAccountContext, use configured handlers for authorization/retry, and call EnsureSuccess(<operation>, ct) for status handling. Do not add authorization to the attachment-download client: its URLs can be pre-signed third-party storage URLs.

## Serialization and cryptography

Use source-generated JSON contexts and pass the required JsonTypeInfo; reflection-based JSON is disabled. Add serializable types to the correct context, including options supplied to libraries such as SignalR. Public plaintext models belong under Models; encrypted server payloads belong under Requests or Responses and use EncString or AsymmetricEncString, not string. Decrypt only when handed a key. Never log, stringify for diagnostics, or persist plaintext. KDF implementations live under Cryptography/KeyDerivation. IDs use the StronglyTypedId definitions in Primitives/Ids/; parse with invariant culture.

## Verification and completion

Run the repository CI build for API changes. Verify the correct source-generated JSON context, account-context attachment, authorization boundary, and plaintext handling.

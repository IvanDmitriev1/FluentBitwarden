# BitwardenApi

## Scope and role

Root instructions apply. This bottom-layer project communicates with Bitwarden servers and represents the crypto primitives in those payloads. It has no dependency on any FluentBitwarden project.

## Local map and HTTP rules

Identity/, Vault/, and Notifications/ implement service APIs; Infrastructure/ contains crypto, serialization, transport, and encoding; Primitives/Ids/ contains strongly typed IDs. Each API area exposes an IXxxApi and an internal sealed implementation using a named HttpClient from [BitwardenApiServiceCollectionExtensions.cs](BitwardenApiServiceCollectionExtensions.cs).

Public API methods take BitwardenAccountContext first and an optional cancellation token last. Attach it with SetBitwardenAccountContext, use configured handlers for authorization/retry, and call EnsureSuccess(<operation>, ct) for status handling. Do not add authorization to the attachment-download client: its URLs can be pre-signed third-party storage URLs.

## Serialization and cryptography

Use source-generated JSON contexts and pass the required JsonTypeInfo; reflection-based JSON is disabled. Add serializable types to the correct context, including options supplied to libraries such as SignalR. Encrypted values are EncString or AsymmetricEncString, not string; decrypt only when handed a key. Never log, stringify for diagnostics, or persist plaintext. KDF implementations live under Infrastructure/Cryptography/Kdf/. IDs use the StronglyTypedId definitions in Primitives/Ids/; parse with invariant culture.

## Verification and completion

Run the repository CI build for API changes. Verify the correct source-generated JSON context, account-context attachment, authorization boundary, and plaintext handling.
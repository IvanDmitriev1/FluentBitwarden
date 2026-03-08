# BitwaredApi Bitwarden Parity Notes

Reference commit: [`8186eb6b9a13c05c94957199c38e909b215fcbb0`](https://github.com/bitwarden/clients/tree/8186eb6b9a13c05c94957199c38e909b215fcbb0)

## Primary references

- `libs/auth/src/common/login-strategies/README.md`
  - Login strategy flow overview and `/connect/token` usage.
- `libs/auth/src/angular/login-via-auth-request/README.md`
  - Device-login initiating-side workflow.
- `libs/common/src/auth/models/request/identity-token/token.request.ts`
  - Shared token form fields: `scope`, `client_id`, device fields, `twoFactor*`, `authRequest`.
- `libs/common/src/auth/models/request/identity-token/password-token.request.ts`
  - Password grant request shape and `grant_type=password`.
- `libs/common/src/auth/models/response/prelogin.response.ts`
  - Prelogin response field names and KDF parameters.
- `libs/common/src/auth/models/response/identity-token.response.ts`
  - Token response field names, decryption-related fields, and nested `UserDecryptionOptions`.
- `libs/common/src/auth/models/response/identity-two-factor.response.ts`
  - `TwoFactorProviders2` shape and email/session token fields.
- `libs/common/src/auth/models/request/auth.request.ts`
  - Auth-request POST payload fields.
- `libs/common/src/auth/models/response/auth-request.response.ts`
  - Auth-request polling response fields and expiry semantics.
- `libs/common/src/auth/enums/two-factor-provider-type.ts`
  - 2FA provider enum values.
- `libs/common/src/auth/enums/auth-request-type.ts`
  - Auth request type enum values.
- `libs/common/src/platform/sync/sync.response.ts`
  - `/sync` response root structure.
- `libs/common/src/vault/models/response/cipher.response.ts`
  - Cipher response field names used for cache indexing and in-memory decryption.
- `libs/common/src/vault/models/response/folder.response.ts`
  - Folder response field names.
- `libs/common/src/key-management/crypto/key-generation/default-key-generation.service.ts`
  - KDF derivation and stretched master-key HKDF expansion.
- `libs/common/src/key-management/master-password/services/master-password.service.ts`
  - Master-password auth hash derivation and user-key unwrap logic.
- `libs/common/src/key-management/crypto/models/enc-string.ts`
  - EncString formats and type parsing.
- `libs/common/src/key-management/crypto/services/encrypt.service.implementation.ts`
  - Symmetric unwrap behavior and HMAC expectations.
- `libs/key-management/src/key.service.ts`
  - Fingerprint derivation inputs and auth-request key-pair handling.
- `libs/auth/src/common/services/auth-request/auth-request-api.service.ts`
  - Auth-request API endpoints and `Device-Identifier` header usage.

## Implemented parity

- Password login uses `POST /accounts/prelogin` followed by `POST /connect/token`.
- Token form serialization matches Bitwarden field naming, including `deviceType`, `deviceIdentifier`, `deviceName`, `twoFactor*`, and `authRequest`.
- Refresh uses `grant_type=refresh_token`.
- Device login uses:
  - `POST /auth-requests/`
  - `GET /auth-requests/{id}/response?code=...`
  - `POST /connect/token` with the access code as `password` and auth request id as `authRequest`.
- Vault sync uses `GET /sync` and stores the encrypted JSON blobs exactly as returned.
- EncString parsing supports the AES-CBC/HMAC and RSA formats needed for password login, vault decrypt, and device-login key unwrap.

## Intentional deviations

- Platform-neutral core library:
  - `BitwaredApi` now targets plain `net8.0`.
  - Windows-only concerns such as app-data paths, DPAPI session protection, device-id persistence, and SQLite caching are owned by the WinUI app.
- No persisted in-flight device-login state:
  - The auth-request private key and access code stay memory-only for the current process.
  - Reason: persisting the ephemeral private key would widen the secret-at-rest footprint for a flow that is not exposed in the WinUI UI yet.
- No cipher create/update/delete in v1:
  - Sync and local reads are implemented first; CRUD stays deferred until parity is verified against the official clients and server behavior.
- Crypto provider choice:
  - `BitwaredApi` uses Bouncy Castle selectively for Argon2id, HKDF, and AES/HMAC-backed EncString decrypt.
  - BCL remains in use for PBKDF2, random generation, RSA import/export, and memory wiping.
- Fingerprint phrase format:
  - Current implementation derives the same fingerprint material inputs as Bitwarden but renders a compact dashed hex phrase instead of the full EFF long-word phrase.
  - Reason: avoids embedding the large EFF word list in this first pass while preserving human comparison of the same derived material.
- No unlock-after-lock flow yet:
  - `LockAsync()` clears the in-memory access token and user key, but the WinUI app does not yet expose a separate unlock screen.
  - Re-authentication is the current path back to decrypted vault access.

## Notes

- The current Bitwarden client code derives a 32-byte master key from the configured KDF and computes the server authorization hash with a single PBKDF2-SHA256 round over that derived key and the raw password.
- A stretched 64-byte master key is still used locally for decrypting the master-key-wrapped user key.

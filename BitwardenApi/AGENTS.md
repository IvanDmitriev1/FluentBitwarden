# AGENTS.md — BitwardenApi

Everything that talks to a Bitwarden server, plus the crypto primitives its payloads are made of. This
project has **no dependency on any FluentBitwarden project** — it is the bottom of the stack, and keeping
it that way is what makes it independently readable and reusable.

Read [the root guide](../AGENTS.md) first.

## Layout

```
Identity/          sign-in, two-factor, tokens, WebAuthn login   (Contracts/ + Internal/)
Vault/
  Items/           sync, cipher CRUD
  Attachments/     attachment metadata and blob download
  Cryptography/    vault key handling
Notifications/     SignalR push channel
Infrastructure/
  Cryptography/    Kdf/ (PBKDF2, Argon2id, HKDF), Enc/ (EncString and converters)
  Serialization/   JSON contexts and converters
  Transport/       HTTP handlers, response helpers, token provider
  Encoding/
Primitives/        value types; Primitives/Ids/ holds the strongly-typed ids
```

## API classes

One `IXxxApi` interface plus an `internal sealed XxxApi` implementation per area, resolved through a
**named** `HttpClient` from [BitwardenApiServiceCollectionExtensions.cs](BitwardenApiServiceCollectionExtensions.cs).

```csharp
internal sealed class FooApi(IHttpClientFactory httpClientFactory) : IFooApi
{
    public async Task<FooResponse> GetFooAsync(
        BitwardenAccountContext accountContext,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = httpClientFactory.CreateVaultClient();

        Uri requestUri = new(accountContext.Environment.ApiBase, "/foo");
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
        requestMessage.SetBitwardenAccountContext(accountContext);

        using var response = await httpClient.SendAsync(requestMessage, cancellationToken);
        response.EnsureSuccess("Foo get", cancellationToken);

        return await response.Content.ReadFromJsonAsync(VaultJsonContext.ConfiguredDefault.FooResponse, cancellationToken)
               ?? throw new BitwardenApiException("Foo get returned no body.");
    }
}
```

Conventions worth keeping:

- Every public method takes `BitwardenAccountContext` first (server environment + identity) and
  `CancellationToken cancellationToken = default` last.
- The account context is attached with `SetBitwardenAccountContext`, never by hand-setting headers.
- Required headers, authorization, and retry are **message handlers / resilience handlers** configured
  once in the registration extension. Do not hand-roll a retry loop or add an `Authorization` header
  inside an API method.
- `EnsureSuccess("<operation>", ct)` for status handling, so failures carry a useful operation name.
- The attachment download client deliberately carries **no** Bitwarden authorization header: it hits
  pre-signed third-party storage URLs, and sending the token there would disclose it. Keep it that way.

## JSON

Source-generated only, and enforced: `JsonSerializerIsReflectionEnabledByDefault=false` in
[Directory.Build.props](../Directory.Build.props) makes the reflection-based overloads throw at runtime,
in Debug as well as in a trimmed publish.

Contexts such as `VaultJsonContext` and `NotificationsJsonContext` list every serializable type. Add
yours to the right context and pass its `JsonTypeInfo` at the call site:

```csharp
await response.Content.ReadFromJsonAsync(VaultJsonContext.ConfiguredDefault.FooResponse, cancellationToken);
```

Same rule for anything handing options to a library — the SignalR connection in
`BitwardenNotificationsApi` sets `PayloadSerializerOptions` to the source-generated context's options
for exactly this reason.

Encrypted values deserialize through the custom converters in `Infrastructure/Serialization` and
`Infrastructure/Cryptography/Enc`.

## Crypto and encrypted values

- KDFs live in `Infrastructure/Cryptography/Kdf/` (PBKDF2, Argon2id, HKDF).
- An encrypted field is an `EncString` (or `AsymmetricEncString`) — never a `string`. They carry the
  encryption type and the ciphertext, convert to/from `byte[]` for storage, and only decrypt when handed a
  key.
- Decryption is the caller's business: this project transports and represents ciphertext, the AppHost's
  vault workspace decrypts it with session keys.
- Never log, stringify for diagnostics, or persist a decrypted value from here.

## Strongly-typed ids

Ids are declared in `Primitives/Ids/*.cs`:

```csharp
[MemoryPackable(GenerateType.NoGenerate)]
[StronglyTypedId(Template.String)]
public readonly partial struct FooId
{
    public bool IsEmpty => string.IsNullOrEmpty(Value);
}
```

`[MemoryPackable(GenerateType.NoGenerate)]` is what lets the id be used inside IPC payloads (with a
`StronglyTypedIdFormatter` on the property) without generating a competing formatter here. Parse with
`FooId.Parse(value, CultureInfo.InvariantCulture)`.

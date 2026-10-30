using BitwardenApi.Cryptography.Encryption;
using BitwardenApi.Vault.Ciphers.Models;
using BitwardenApi.Vault.Internal;
using System.Text.Json.Serialization;

namespace BitwardenApi.Vault.Ciphers.Requests;

/// <summary>
/// Structured request body for <c>POST /ciphers</c> and <c>PUT /ciphers/{id}</c>.
/// Mirrors Bitwarden server's <c>CipherRequestModel</c> DTO; exposed here as
/// <see cref="CipherRequest"/>. The server derives the stored
/// <c>data</c> blob from these fields. Encrypted fields are <see cref="EncString"/> and
/// serialize to the wire form <c>"2.iv|data|mac"</c>.
/// </summary>
public sealed class CipherRequest
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public CipherId Id { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonConverter(typeof(OptionalOrganizationIdJsonConverter))]
    public OrganizationId OrganizationId { get; init; }

    public required CipherType Type { get; init; }

    /// <summary>
    /// The user the payload is encrypted for; the server validates it matches the caller.
    /// For personal ciphers this is the current user id.
    /// </summary>
    public Guid? EncryptedFor { get; init; }

    /// <summary>Owning folder id (personal ciphers only); empty when unfiled.</summary>
    [JsonConverter(typeof(OptionalFolderIdJsonConverter))]
    public FolderId FolderId { get; init; }

    public required bool Favorite { get; init; }

    /// <summary>Password re-prompt: 0 = none, 1 = password.</summary>
    public required int Reprompt { get; init; }

    /// <summary>The individual cipher key, wrapped by the vault key.</summary>
    public required EncString Key { get; init; }

    public required EncString Name { get; init; }
    public EncString Notes { get; init; }

    public CipherLoginRequest? Login { get; init; }
    public CipherCardRequest? Card { get; init; }
    public CipherIdentityRequest? Identity { get; init; }
    public CipherSecureNoteRequest? SecureNote { get; init; }
    public CipherSshKeyRequest? SshKey { get; init; }

    /// <summary>Optimistic-concurrency guard sent on update; the last revision date we hold.</summary>
    public DateTime? LastKnownRevisionDate { get; init; }
}

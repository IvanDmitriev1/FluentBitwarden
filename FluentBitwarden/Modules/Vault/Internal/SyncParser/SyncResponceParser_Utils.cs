using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Modules.Vault.Models;
using FluentBitwarden.Modules.Vault.Abstractions;
using System.Text.Json;

namespace FluentBitwarden.Modules.Vault.Internal.SyncParser;

internal partial class VaultSyncResponseParser
{
    private delegate void CaptureVisitor<TState, TProperty>(
        ref Utf8JsonReader reader,
        int depth,
        ref TState state,
        ref TProperty currentProperty)
        where TState : struct
        where TProperty : struct;

    private delegate void PersistVisitor<TState>(
        IVaultWriterRepository writer,
        ref TState state)
        where TState : struct;

    private void CaptureCurrentObject<TState, TProperty>(
        ref Utf8JsonReader reader,
        ref TState state,
        ref TProperty currentProperty,
        CaptureVisitor<TState, TProperty> visitor,
        PersistVisitor<TState> persist)
        where TState : struct
        where TProperty : struct
    {
        if (!_objectCaptureState.IsActive)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Expected StartObject, got {reader.TokenType}.");
            }

            _objectCaptureState.IsActive = true;
            _objectCaptureState.Depth = 0;
            _cipherPayloadCapture.Reset();
            state = default;
            currentProperty = default;
        }

        if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
        {
            _objectCaptureState.Depth++;
        }

        visitor.Invoke(ref reader, _objectCaptureState.Depth, ref state, ref currentProperty);

        if (reader.TokenType is JsonTokenType.EndObject or JsonTokenType.EndArray)
        {
            _objectCaptureState.Depth--;
        }

        if (_objectCaptureState.Depth == 0)
        {
            persist.Invoke(dataWriter, ref state);
            _objectCaptureState.IsActive = false;
        }
    }

    private void CaptureArray<TState, TProperty>(
        ref Utf8JsonReader reader,
        ref TState state,
        ref TProperty currentProperty,
        CaptureVisitor<TState, TProperty> visitor,
        PersistVisitor<TState> persist)
        where TState : struct
        where TProperty : struct
    {
        if (!_arrayCaptureState.IsActive)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException($"Expected StartArray, got {reader.TokenType}.");
            }

            _arrayCaptureState.IsActive = true;
            _arrayCaptureState.ProcessedItems = 0;
            return;
        }

        if (reader.TokenType == JsonTokenType.EndArray && !_objectCaptureState.IsActive)
        {
            _arrayCaptureState = default;
            return;
        }

        if (!_objectCaptureState.IsActive && reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException($"Expected array items to be JSON objects, got {reader.TokenType}.");
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            _arrayCaptureState.ProcessedItems++;
        }

        CaptureCurrentObject(ref reader, ref state, ref currentProperty, visitor, persist);
    }

    private static FolderId ParseFolderId(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected folder id string, got {reader.TokenType}.");
        }

        Span<char> buffer = stackalloc char[64];
        var charsWritten = reader.CopyString(buffer);
        return FolderId.Parse(buffer[..charsWritten]);
    }

    private static CipherId ParseCipherId(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected vaultCipher id string, got {reader.TokenType}.");
        }

        Span<char> buffer = stackalloc char[64];
        var charsWritten = reader.CopyString(buffer);
        return CipherId.Parse(buffer[..charsWritten]);
    }

    private static CollectionId ParseCollectionId(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected collection id string, got {reader.TokenType}.");
        }

        Span<char> buffer = stackalloc char[64];
        var charsWritten = reader.CopyString(buffer);
        return CollectionId.Parse(buffer[..charsWritten]);
    }

    private static string ParseString(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected string, got {reader.TokenType}.");
        }

        return reader.GetString()!;
    }

    private static OrganizationId? ParseNullableOrganizationId(ref Utf8JsonReader reader)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.String => ParseOrganizationId(ref reader),
            _ => throw new JsonException($"Expected organization id string or null, got {reader.TokenType}.")
        };
    }

    private static OrganizationId ParseOrganizationId(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected organization id string, got {reader.TokenType}.");
        }

        Span<char> buffer = stackalloc char[64];
        var charsWritten = reader.CopyString(buffer);
        return OrganizationId.Parse(buffer[..charsWritten]);
    }

    private static DateTimeOffset ParseDateTimeOffset(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected date string, got {reader.TokenType}.");
        }

        return reader.GetDateTimeOffset();
    }

    private static DateTimeOffset? ParseNullableDateTimeOffset(ref Utf8JsonReader reader)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.String => reader.GetDateTimeOffset(),
            _ => throw new JsonException($"Expected date string or null, got {reader.TokenType}.")
        };
    }

    private static bool ParseBooleanOrIntFlag(ref Utf8JsonReader reader)
    {
        return reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Number => reader.GetInt32() != 0,
            _ => throw new JsonException($"Expected boolean or numeric flag, got {reader.TokenType}.")
        };
    }

    private static int ParseInt(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.Number)
        {
            throw new JsonException($"Expected integer number, got {reader.TokenType}.");
        }

        return reader.GetInt32();
    }

    private static int? ParseNullableInt(ref Utf8JsonReader reader)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.Number => reader.GetInt32(),
            _ => throw new JsonException($"Expected number or null, got {reader.TokenType}.")
        };
    }

    private static void EnsureFolderIsComplete(ref readonly VaultFolderDto dto)
    {
        if (dto.Id == default)
        {
            throw new InvalidDataException("VaultFolder payload did not include an id.");
        }

        if (dto.RevisionDate == default)
        {
            throw new InvalidDataException("VaultFolder payload did not include a revision date.");
        }

        if (dto.EncryptedName is null)
        {
            throw new InvalidDataException("VaultFolder payload did not include a name.");
        }
    }

    private static void EnsureCollectionIsComplete(ref readonly VaultCollectionDto dto)
    {
        if (dto.Id == default)
        {
            throw new InvalidDataException("Collection payload did not include an id.");
        }

        if (dto.EncryptedName is null)
        {
            throw new InvalidDataException("Collection payload did not include a name.");
        }
    }

    private void EnsureCipherIsComplete(ref readonly VaultCipherDto dto)
    {
        if (dto.Id == default)
        {
            throw new InvalidDataException("VaultCipher payload did not include an id.");
        }

        if (!Enum.IsDefined(dto.CipherType) || dto.CipherType == 0)
        {
            throw new InvalidDataException($"VaultCipher payload did not include a supported type. Parsed value: {(int)dto.CipherType}.");
        }

        if (dto.RevisionDate == default)
        {
            throw new InvalidDataException("VaultCipher payload did not include a revision date.");
        }

        if (dto.CreationDate == default)
        {
            throw new InvalidDataException("VaultCipher payload did not include a creation date.");
        }

        if (!_cipherPayloadCapture.HasCapturedPayload)
        {
            throw new InvalidDataException("VaultCipher payload did not include the root data property.");
        }
    }
}

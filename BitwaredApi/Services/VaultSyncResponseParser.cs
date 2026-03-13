using System.Text;
using System.Text.Json;
using BitwaredApi.Abstractions.Exceptions;
using BitwaredApi.Extensions;
using BitwaredApi.Models.Vault;
using CommunityToolkit.HighPerformance.Buffers;

namespace BitwaredApi.Services;

internal static class VaultSyncResponseParser
{
    private const int InitialBufferSize = 64 * 1024;

    public static async ValueTask<EncryptedSyncSnapshot> CreateSnapshotAsync(
        Stream stream,
        VaultSyncRequest request,
        DateTimeOffset lastSyncUtc,
        DateTimeOffset? revisionDate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(request);

        List<CipherSyncItem> ciphers = [];
        List<FolderSyncItem> folders = [];
        List<CollectionSyncItem> collections = [];

        using ArrayPoolBufferWriter<byte> bufferWriter = new(InitialBufferSize);

        bool isFinalBlock = false;
        int bytesInBuffer = 0;
        JsonReaderState readerState = default;
        bool rootStarted = false;
        bool rootCompleted = false;
        SyncSection currentSection = SyncSection.None;
        string? pendingRootProperty = null;
        int skippedRootValueDepth = 0;
        CurrentSyncItemState? currentItem = null;

        try
        {
            while (!isFinalBlock || bytesInBuffer > 0)
            {
                if (!isFinalBlock)
                {
                    Memory<byte> readBuffer = bufferWriter.GetMemory(1);
                    int read = await stream.ReadAsync(readBuffer, cancellationToken).ConfigureAwait(false);
                    if (read == 0)
                    {
                        isFinalBlock = true;
                    }
                    else
                    {
                        bufferWriter.Advance(read);
                        bytesInBuffer += read;
                    }
                }

                Utf8JsonReader reader = new(bufferWriter.WrittenSpan, isFinalBlock, readerState);

                while (reader.Read())
                {
                    ProcessToken(
                        ref reader,
                        ref rootStarted,
                        ref rootCompleted,
                        ref currentSection,
                        ref pendingRootProperty,
                        ref skippedRootValueDepth,
                        ref currentItem,
                        ciphers,
                        folders,
                        collections,
                        bufferWriter.WrittenSpan);
                }

                int consumed = checked((int)reader.BytesConsumed);

                if (currentItem is not null && consumed > currentItem.StartOffset)
                {
                    currentItem.AppendParsedPrefix(bufferWriter.WrittenSpan.Slice(
                        currentItem.StartOffset,
                        consumed - currentItem.StartOffset));
                    currentItem.StartOffset = 0;
                }

                bytesInBuffer -= consumed;
                bufferWriter.CompactUnreadBytes(consumed, bytesInBuffer);
                readerState = reader.CurrentState;

                if (isFinalBlock)
                {
                    break;
                }
            }
        }
        catch (JsonException ex)
        {
            throw new ServerVersionMismatchException("Sync response payload was not a supported JSON object.", ex);
        }
        finally
        {
            currentItem?.Dispose();
        }

        if (!rootStarted
            || !rootCompleted
            || currentSection != SyncSection.None
            || pendingRootProperty is not null
            || skippedRootValueDepth != 0
            || currentItem is not null)
        {
            throw new ServerVersionMismatchException("Sync response ended unexpectedly.");
        }

        return new EncryptedSyncSnapshot(
            new VaultAccountRecord(
                request.AccountId,
                request.Email,
                request.Environment.ApiBase.ToString(),
                request.Environment.IdentityBase.ToString(),
                lastSyncUtc,
                lastSyncUtc),
            new VaultSyncStateRecord(
                request.AccountId,
                revisionDate,
                lastSyncUtc,
                ciphers.Count,
                folders.Count,
                collections.Count),
            ciphers,
            folders,
            collections);
    }

    private static void ProcessToken(
        ref Utf8JsonReader reader,
        ref bool rootStarted,
        ref bool rootCompleted,
        ref SyncSection currentSection,
        ref string? pendingRootProperty,
        ref int skippedRootValueDepth,
        ref CurrentSyncItemState? currentItem,
        List<CipherSyncItem> ciphers,
        List<FolderSyncItem> folders,
        List<CollectionSyncItem> collections,
        ReadOnlySpan<byte> buffer)
    {
        if (currentItem is not null)
        {
            ProcessItemToken(
                ref reader,
                ref currentItem,
                ciphers,
                folders,
                collections,
                buffer);
            return;
        }

        if (!rootStarted)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new ServerVersionMismatchException("Sync response root was not a JSON object.");
            }

            rootStarted = true;
            return;
        }

        if (rootCompleted)
        {
            throw new ServerVersionMismatchException("Sync response contained unexpected data after the root object.");
        }

        if (currentSection != SyncSection.None)
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                currentSection = SyncSection.None;
                return;
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new ServerVersionMismatchException("Sync response section contained a non-object item.");
            }

            currentItem = new CurrentSyncItemState(currentSection, checked((int)reader.TokenStartIndex));
            return;
        }

        if (skippedRootValueDepth > 0)
        {
            UpdateDepth(ref skippedRootValueDepth, reader.TokenType);
            return;
        }

        if (pendingRootProperty is not null)
        {
            HandleRootPropertyValue(
                ref reader,
                pendingRootProperty,
                ref currentSection,
                ref skippedRootValueDepth);
            pendingRootProperty = null;
            return;
        }

        if (reader.TokenType == JsonTokenType.PropertyName)
        {
            if (reader.CurrentDepth != 1)
            {
                throw new ServerVersionMismatchException("Sync response contained an unexpected nested root property.");
            }

            pendingRootProperty = reader.GetString()
                ?? throw new ServerVersionMismatchException("Sync response property name was missing.");
            return;
        }

        if (reader.TokenType == JsonTokenType.EndObject)
        {
            rootCompleted = true;
            return;
        }

        throw new ServerVersionMismatchException("Sync response contained an unexpected root token.");
    }

    private static void ProcessItemToken(
        ref Utf8JsonReader reader,
        ref CurrentSyncItemState? currentItem,
        List<CipherSyncItem> ciphers,
        List<FolderSyncItem> folders,
        List<CollectionSyncItem> collections,
        ReadOnlySpan<byte> buffer)
    {
        CurrentSyncItemState item = currentItem
            ?? throw new InvalidOperationException("Current sync item was not initialized.");
        bool collectionIdsStarted = false;

        if (item.CollectionIdsArrayDepth > 0)
        {
            ValidateCollectionIdsToken(item, reader.TokenType);
        }
        else if (reader.TokenType == JsonTokenType.PropertyName && item.Nesting == 1)
        {
            item.PendingProperty = reader.GetString()
                ?? throw new ServerVersionMismatchException("Sync item property name was missing.");
        }
        else if (item.PendingProperty is not null)
        {
            collectionIdsStarted = ReadItemValue(ref reader, item);
        }

        int itemNesting = item.Nesting;
        UpdateDepth(ref itemNesting, reader.TokenType);
        item.Nesting = itemNesting;

        if (item.CollectionIdsArrayDepth > 0 && !collectionIdsStarted)
        {
            int collectionIdsDepth = item.CollectionIdsArrayDepth;
            UpdateDepth(ref collectionIdsDepth, reader.TokenType);
            item.CollectionIdsArrayDepth = collectionIdsDepth;

            if (item.CollectionIdsArrayDepth == 0)
            {
                item.CollectionIdsLength = item.GetRelativeOffset(reader.BytesConsumed) - item.CollectionIdsStartOffset;
            }
        }

        if (reader.TokenType == JsonTokenType.EndObject && item.Nesting == 0)
        {
            int endOffset = checked((int)reader.BytesConsumed);

            switch (item.Section)
            {
                case SyncSection.Ciphers:
                    ciphers.Add(CreateCipherItem(item, buffer, endOffset));
                    break;
                case SyncSection.Folders:
                    folders.Add(CreateFolderItem(item, buffer, endOffset));
                    break;
                case SyncSection.Collections:
                    collections.Add(CreateCollectionItem(item, buffer, endOffset));
                    break;
                default:
                    throw new ServerVersionMismatchException("Sync response contained an unsupported section.");
            }

            item.Dispose();
            currentItem = null;
        }
    }

    private static void HandleRootPropertyValue(
        ref Utf8JsonReader reader,
        string propertyName,
        ref SyncSection currentSection,
        ref int skippedRootValueDepth)
    {
        SyncSection section = propertyName switch
        {
            "ciphers" => SyncSection.Ciphers,
            "folders" => SyncSection.Folders,
            "collections" => SyncSection.Collections,
            _ => SyncSection.None,
        };

        if (section != SyncSection.None)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return;
            }

            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new ServerVersionMismatchException($"Sync response property '{propertyName}' was not an array.");
            }

            currentSection = section;
            return;
        }

        if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
        {
            skippedRootValueDepth = 1;
        }
    }

    private static bool ReadItemValue(ref Utf8JsonReader reader, CurrentSyncItemState item)
    {
        string propertyName = item.PendingProperty
            ?? throw new InvalidOperationException("Pending sync item property was not set.");
        item.PendingProperty = null;

        switch (propertyName)
        {
            case "id":
                item.Id = ReadRequiredString(ref reader, $"Sync {item.Section.ToString().ToLowerInvariant()} payload property 'id' was not a string.");
                return false;
            case "type":
                item.Type = ReadRequiredInt32(ref reader, "Sync cipher payload property 'type' was not a number.");
                return false;
            case "organizationId":
                item.OrganizationId = ReadOptionalString(ref reader, "Sync cipher payload property 'organizationId' was not a string.");
                return false;
            case "folderId":
                item.FolderId = ReadOptionalString(ref reader, "Sync cipher payload property 'folderId' was not a string.");
                return false;
            case "revisionDate":
                item.RevisionDate = ReadOptionalDateTimeOffset(
                    ref reader,
                    $"Sync {item.Section.ToString().ToLowerInvariant()} payload property 'revisionDate' was not a valid date-time string.");
                return false;
            case "collectionIds":
                return ReadCollectionIdsValue(ref reader, item);
            default:
                return false;
        }
    }

    private static bool ReadCollectionIdsValue(ref Utf8JsonReader reader, CurrentSyncItemState item)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            item.CollectionIdsStartOffset = -1;
            item.CollectionIdsLength = 0;
            item.CollectionIdsArrayDepth = 0;
            return false;
        }

        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new ServerVersionMismatchException("Sync cipher payload property 'collectionIds' was not an array.");
        }

        item.CollectionIdsStartOffset = item.GetRelativeOffset(reader.TokenStartIndex);
        item.CollectionIdsLength = -1;
        item.CollectionIdsArrayDepth = 1;
        return true;
    }

    private static void ValidateCollectionIdsToken(CurrentSyncItemState item, JsonTokenType tokenType)
    {
        if (item.CollectionIdsArrayDepth != 1)
        {
            return;
        }

        if (tokenType is JsonTokenType.EndArray or JsonTokenType.String)
        {
            return;
        }

        throw new ServerVersionMismatchException(
            "Sync cipher payload property 'collectionIds' contained a non-string value.");
    }

    private static CipherSyncItem CreateCipherItem(
        CurrentSyncItemState item,
        ReadOnlySpan<byte> buffer,
        int endOffset)
    {
        byte[] payload = item.BuildPayload(buffer, endOffset);

        return new CipherSyncItem(
            item.Id ?? throw new ServerVersionMismatchException("Sync cipher payload did not include an Id."),
            item.Type ?? throw new ServerVersionMismatchException("Sync cipher payload did not include a type."),
            item.OrganizationId,
            item.FolderId,
            item.CollectionIdsStartOffset >= 0 && item.CollectionIdsLength >= 0
                ? Encoding.UTF8.GetString(payload.AsSpan(item.CollectionIdsStartOffset, item.CollectionIdsLength))
                : "[]",
            item.RevisionDate,
            payload);
    }

    private static FolderSyncItem CreateFolderItem(
        CurrentSyncItemState item,
        ReadOnlySpan<byte> buffer,
        int endOffset)
        => new(
            item.Id ?? throw new ServerVersionMismatchException("Sync folder payload did not include an Id."),
            item.RevisionDate,
            item.BuildPayload(buffer, endOffset));

    private static CollectionSyncItem CreateCollectionItem(
        CurrentSyncItemState item,
        ReadOnlySpan<byte> buffer,
        int endOffset)
        => new(
            item.Id ?? throw new ServerVersionMismatchException("Sync collection payload did not include an Id."),
            item.RevisionDate,
            item.BuildPayload(buffer, endOffset));

    private static string ReadRequiredString(ref Utf8JsonReader reader, string errorMessage)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new ServerVersionMismatchException(errorMessage);
        }

        return reader.GetString() ?? throw new ServerVersionMismatchException(errorMessage);
    }

    private static string? ReadOptionalString(ref Utf8JsonReader reader, string errorMessage)
        => reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.String => reader.GetString(),
            _ => throw new ServerVersionMismatchException(errorMessage),
        };

    private static int ReadRequiredInt32(ref Utf8JsonReader reader, string errorMessage)
    {
        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetInt32(out int value))
        {
            throw new ServerVersionMismatchException(errorMessage);
        }

        return value;
    }

    private static DateTimeOffset? ReadOptionalDateTimeOffset(ref Utf8JsonReader reader, string errorMessage)
        => reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.String when reader.TryGetDateTimeOffset(out DateTimeOffset parsed) => parsed,
            _ => throw new ServerVersionMismatchException(errorMessage),
        };

    private static void UpdateDepth(ref int depth, JsonTokenType tokenType)
    {
        switch (tokenType)
        {
            case JsonTokenType.StartObject:
            case JsonTokenType.StartArray:
                depth++;
                break;
            case JsonTokenType.EndObject:
            case JsonTokenType.EndArray:
                depth--;
                break;
        }
    }

    private enum SyncSection
    {
        None,
        Ciphers,
        Folders,
        Collections,
    }

    private sealed class CurrentSyncItemState(SyncSection section, int startOffset) : IDisposable
    {
        private ArrayPoolBufferWriter<byte>? _committedBytes;

        public SyncSection Section { get; } = section;

        public int StartOffset { get; set; } = startOffset;

        public int Nesting { get; set; } = 1;

        public string? PendingProperty { get; set; }

        public string? Id { get; set; }

        public int? Type { get; set; }

        public string? OrganizationId { get; set; }

        public string? FolderId { get; set; }

        public DateTimeOffset? RevisionDate { get; set; }

        public int CollectionIdsStartOffset { get; set; } = -1;

        public int CollectionIdsLength { get; set; } = -1;

        public int CollectionIdsArrayDepth { get; set; }

        public int CommittedLength => _committedBytes?.WrittenCount ?? 0;

        public void AppendParsedPrefix(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty)
            {
                return;
            }

            _committedBytes ??= new ArrayPoolBufferWriter<byte>(data.Length);
            data.CopyTo(_committedBytes.GetSpan(data.Length));
            _committedBytes.Advance(data.Length);
        }

        public int GetRelativeOffset(long localOffset)
            => checked(CommittedLength + (int)localOffset - StartOffset);

        public byte[] BuildPayload(ReadOnlySpan<byte> buffer, int endOffset)
        {
            int tailLength = endOffset - StartOffset;
            int totalLength = CommittedLength + tailLength;
            byte[] payload = GC.AllocateUninitializedArray<byte>(totalLength);

            int destinationOffset = 0;
            if (_committedBytes is not null)
            {
                _committedBytes.WrittenSpan.CopyTo(payload);
                destinationOffset = _committedBytes.WrittenCount;
            }

            if (tailLength > 0)
            {
                buffer.Slice(StartOffset, tailLength).CopyTo(payload.AsSpan(destinationOffset));
            }

            return payload;
        }

        public void Dispose()
        {
            _committedBytes?.Dispose();
        }
    }
}

using System.Text;
using System.Text.Json;
using BitwaredApi.Abstractions;
using BitwaredApi.Abstractions.Exceptions;
using BitwaredApi.Extensions;
using BitwaredApi.Models.Vault;
using BitwaredApi.Utils;
using CommunityToolkit.HighPerformance.Buffers;

namespace BitwaredApi.Services;

internal static class VaultSyncResponseParser
{
    public static async ValueTask<SyncPayloadCounts> WriteToStoreAsync(
        Stream stream,
        IVaultSyncWriteSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(session);

        SyncParseState state = new(session);

        try
        {
            await Utf8JsonStreamParser.ParseAsync(
                stream,
                state,
                ProcessPass,
                CompletePassAsync,
                cancellationToken).ConfigureAwait(false);

            state.ValidateCompleted();
            return new SyncPayloadCounts(state.CipherCount, state.FolderCount, state.CollectionCount);
        }
        catch (JsonException ex)
        {
            throw new ServerVersionMismatchException("Sync response payload was not a supported JSON object.", ex);
        }
        finally
        {
            state.Dispose();
        }
    }

    private static void ProcessPass(
        SyncParseState state,
        ref Utf8JsonReader reader,
        ReadOnlySpan<byte> buffer)
    {
        SyncParseFrame frame = new(state, buffer);

        while (reader.Read())
        {
            if (frame.ProcessToken(ref reader))
            {
                break;
            }
        }

        state.PendingWrite = frame.PendingWrite;
    }

    private static ValueTask CompletePassAsync(
        SyncParseState state,
        ReadOnlyMemory<byte> buffer,
        long bytesConsumed,
        CancellationToken cancellationToken)
        => state.CompletePassAsync(buffer, bytesConsumed, cancellationToken);

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
                item.Id = reader.ReadRequiredString($"Sync {item.Section.ToString().ToLowerInvariant()} payload property 'id' was not a string.");
                return false;

            case "type":
                item.Type = reader.ReadRequiredInt32("Sync cipher payload property 'type' was not a number.");
                return false;

            case "organizationId":
                item.OrganizationId = reader.ReadOptionalString("Sync cipher payload property 'organizationId' was not a string.");
                return false;

            case "folderId":
                item.FolderId = reader.ReadOptionalString("Sync cipher payload property 'folderId' was not a string.");
                return false;

            case "revisionDate":
                item.RevisionDate = reader.ReadOptionalDateTimeOffset(
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

    private static PendingVaultWrite CreateCipherWrite(
        CurrentSyncItemState item,
        ReadOnlySpan<byte> buffer,
        int endOffset)
    {
        PooledPayload payload = item.BuildPayload(buffer, endOffset);

        return new PendingVaultWrite(
            SyncSection.Ciphers,
            payload,
            new CipherSyncItem(
                item.Id ?? throw new ServerVersionMismatchException("Sync cipher payload did not include an Id."),
                item.Type ?? throw new ServerVersionMismatchException("Sync cipher payload did not include a type."),
                item.OrganizationId,
                item.FolderId,
                item.CollectionIdsStartOffset >= 0 && item.CollectionIdsLength >= 0
                    ? Encoding.UTF8.GetString(payload.Memory.Span.Slice(item.CollectionIdsStartOffset, item.CollectionIdsLength))
                    : "[]",
                item.RevisionDate),
            folder: null,
            collection: null);
    }

    private static PendingVaultWrite CreateFolderWrite(
        CurrentSyncItemState item,
        ReadOnlySpan<byte> buffer,
        int endOffset)
    {
        PooledPayload payload = item.BuildPayload(buffer, endOffset);

        return new PendingVaultWrite(
            SyncSection.Folders,
            payload,
            cipher: null,
            new FolderSyncItem(
                item.Id ?? throw new ServerVersionMismatchException("Sync folder payload did not include an Id."),
                item.RevisionDate),
            collection: null);
    }

    private static PendingVaultWrite CreateCollectionWrite(
        CurrentSyncItemState item,
        ReadOnlySpan<byte> buffer,
        int endOffset)
    {
        PooledPayload payload = item.BuildPayload(buffer, endOffset);

        return new PendingVaultWrite(
            SyncSection.Collections,
            payload,
            cipher: null,
            folder: null,
            new CollectionSyncItem(
                item.Id ?? throw new ServerVersionMismatchException("Sync collection payload did not include an Id."),
                item.RevisionDate));
    }

    public readonly record struct SyncPayloadCounts(
        int CipherCount,
        int FolderCount,
        int CollectionCount);

    private enum SyncSection
    {
        None,
        Ciphers,
        Folders,
        Collections,
    }

    private sealed class PendingVaultWrite(
        SyncSection section,
        PooledPayload payload,
        CipherSyncItem? cipher,
        FolderSyncItem? folder,
        CollectionSyncItem? collection) : IDisposable
    {
        public SyncSection Section { get; } = section;

        public async ValueTask WriteAsync(IVaultSyncWriteSession session, CancellationToken cancellationToken)
        {
            switch (Section)
            {
                case SyncSection.Ciphers:
                    await session.WriteCipherAsync(
                        cipher ?? throw new InvalidOperationException("Cipher payload was missing."),
                        payload.Memory,
                        cancellationToken).ConfigureAwait(false);
                    break;

                case SyncSection.Folders:
                    await session.WriteFolderAsync(
                        folder ?? throw new InvalidOperationException("Folder payload was missing."),
                        payload.Memory,
                        cancellationToken).ConfigureAwait(false);
                    break;

                case SyncSection.Collections:
                    await session.WriteCollectionAsync(
                        collection ?? throw new InvalidOperationException("Collection payload was missing."),
                        payload.Memory,
                        cancellationToken).ConfigureAwait(false);
                    break;

                default:
                    throw new InvalidOperationException("Unsupported sync section write.");
            }
        }

        public void Dispose()
            => payload.Dispose();
    }

    private sealed class SyncParseState(IVaultSyncWriteSession session) : IDisposable
    {
        private readonly IVaultSyncWriteSession _session = session;

        public bool RootStarted { get; set; }
        public bool RootCompleted { get; set; }
        public SyncSection CurrentSection { get; set; }
        public string? PendingRootProperty { get; set; }
        public int SkippedRootValueDepth { get; set; }
        public CurrentSyncItemState? CurrentItem { get; set; }
        public PendingVaultWrite? PendingWrite { get; set; }
        public int CipherCount { get; private set; }
        public int FolderCount { get; private set; }
        public int CollectionCount { get; private set; }

        public void AppendParsedPrefix(ReadOnlySpan<byte> buffer, long bytesConsumed)
        {
            CurrentSyncItemState? currentItem = CurrentItem;
            int consumed = checked((int)bytesConsumed);

            if (currentItem is null || consumed <= currentItem.StartOffset)
            {
                return;
            }

            currentItem.AppendParsedPrefix(
                buffer.Slice(
                    currentItem.StartOffset,
                    consumed - currentItem.StartOffset));
            currentItem.StartOffset = 0;
        }

        public async ValueTask CompletePassAsync(
            ReadOnlyMemory<byte> buffer,
            long bytesConsumed,
            CancellationToken cancellationToken)
        {
            AppendParsedPrefix(buffer.Span, bytesConsumed);

            PendingVaultWrite? pendingWrite = PendingWrite;
            if (pendingWrite is null)
            {
                return;
            }

            PendingWrite = null;
            SyncSection section = pendingWrite.Section;

            using (pendingWrite)
            {
                await pendingWrite.WriteAsync(_session, cancellationToken).ConfigureAwait(false);
            }

            RecordWrite(section);
        }

        public void ValidateCompleted()
        {
            if (!RootStarted
                || !RootCompleted
                || CurrentSection != SyncSection.None
                || PendingRootProperty is not null
                || SkippedRootValueDepth != 0
                || CurrentItem is not null
                || PendingWrite is not null)
            {
                throw new ServerVersionMismatchException("Sync response payload ended unexpectedly.");
            }
        }

        private void RecordWrite(SyncSection section)
        {
            switch (section)
            {
                case SyncSection.Ciphers:
                    CipherCount++;
                    break;

                case SyncSection.Folders:
                    FolderCount++;
                    break;

                case SyncSection.Collections:
                    CollectionCount++;
                    break;

                default:
                    throw new InvalidOperationException("Unsupported sync section was completed.");
            }
        }

        public void Dispose()
        {
            PendingWrite?.Dispose();
            PendingWrite = null;
            CurrentItem?.Dispose();
            CurrentItem = null;
        }
    }

    private ref struct SyncParseFrame(SyncParseState state, ReadOnlySpan<byte> buffer)
    {
        public PendingVaultWrite? PendingWrite { get; private set; }
        private SyncParseState State { get; } = state;
        private ReadOnlySpan<byte> Buffer { get; } = buffer;

        public bool ProcessToken(ref Utf8JsonReader reader)
        {
            PendingWrite = null;

            if (State.CurrentItem is not null)
            {
                return ProcessItemToken(ref reader);
            }

            if (!State.RootStarted)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new ServerVersionMismatchException("Sync response root was not a JSON object.");
                }

                State.RootStarted = true;
                return false;
            }

            if (State.RootCompleted)
            {
                throw new ServerVersionMismatchException("Sync response contained unexpected data after the root object.");
            }

            if (State.CurrentSection != SyncSection.None)
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    State.CurrentSection = SyncSection.None;
                    return false;
                }

                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new ServerVersionMismatchException("Sync response section contained a non-object item.");
                }

                State.CurrentItem = new CurrentSyncItemState(State.CurrentSection, checked((int)reader.TokenStartIndex));
                return false;
            }

            if (State.SkippedRootValueDepth > 0)
            {
                int skippedRootValueDepth = State.SkippedRootValueDepth;
                Utf8JsonStreamParser.UpdateDepth(ref skippedRootValueDepth, reader.TokenType);
                State.SkippedRootValueDepth = skippedRootValueDepth;
                return false;
            }

            if (State.PendingRootProperty is not null)
            {
                SyncSection currentSection = State.CurrentSection;
                int skippedRootValueDepth = State.SkippedRootValueDepth;

                HandleRootPropertyValue(
                    ref reader,
                    State.PendingRootProperty,
                    ref currentSection,
                    ref skippedRootValueDepth);

                State.CurrentSection = currentSection;
                State.SkippedRootValueDepth = skippedRootValueDepth;
                State.PendingRootProperty = null;
                return false;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                if (reader.CurrentDepth != 1)
                {
                    throw new ServerVersionMismatchException("Sync response contained an unexpected nested root property.");
                }

                State.PendingRootProperty = reader.GetString()
                    ?? throw new ServerVersionMismatchException("Sync response property name was missing.");
                return false;
            }

            if (reader.TokenType == JsonTokenType.EndObject)
            {
                State.RootCompleted = true;
                return false;
            }

            throw new ServerVersionMismatchException("Sync response contained an unexpected root token.");
        }

        private bool ProcessItemToken(ref Utf8JsonReader reader)
        {
            CurrentSyncItemState item = State.CurrentItem
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
            Utf8JsonStreamParser.UpdateDepth(ref itemNesting, reader.TokenType);
            item.Nesting = itemNesting;

            if (item.CollectionIdsArrayDepth > 0 && !collectionIdsStarted)
            {
                int collectionIdsDepth = item.CollectionIdsArrayDepth;
                Utf8JsonStreamParser.UpdateDepth(ref collectionIdsDepth, reader.TokenType);
                item.CollectionIdsArrayDepth = collectionIdsDepth;

                if (item.CollectionIdsArrayDepth == 0)
                {
                    item.CollectionIdsLength = item.GetRelativeOffset(reader.BytesConsumed) - item.CollectionIdsStartOffset;
                }
            }

            if (reader.TokenType != JsonTokenType.EndObject || item.Nesting != 0)
            {
                return false;
            }

            int endOffset = checked((int)reader.BytesConsumed);
            PendingWrite = item.Section switch
            {
                SyncSection.Ciphers => CreateCipherWrite(item, Buffer, endOffset),
                SyncSection.Folders => CreateFolderWrite(item, Buffer, endOffset),
                SyncSection.Collections => CreateCollectionWrite(item, Buffer, endOffset),
                _ => throw new ServerVersionMismatchException("Sync response contained an unsupported section."),
            };

            item.Dispose();
            State.CurrentItem = null;
            return true;
        }
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

        public PooledPayload BuildPayload(ReadOnlySpan<byte> buffer, int endOffset)
        {
            int tailLength = endOffset - StartOffset;
            int totalLength = CommittedLength + tailLength;
            MemoryOwner<byte> owner = MemoryOwner<byte>.Allocate(totalLength);
            Span<byte> destination = owner.Span[..totalLength];

            int destinationOffset = 0;
            if (_committedBytes is not null)
            {
                _committedBytes.WrittenSpan.CopyTo(destination);
                destinationOffset = _committedBytes.WrittenCount;
            }

            if (tailLength > 0)
            {
                buffer.Slice(StartOffset, tailLength).CopyTo(destination[destinationOffset..]);
            }

            return new PooledPayload(owner, totalLength);
        }

        public void Dispose()
            => _committedBytes?.Dispose();
    }

    private sealed class PooledPayload(MemoryOwner<byte> owner, int length) : IDisposable
    {
        public ReadOnlyMemory<byte> Memory => owner.Memory[..length];

        public void Dispose()
            => owner.Dispose();
    }
}

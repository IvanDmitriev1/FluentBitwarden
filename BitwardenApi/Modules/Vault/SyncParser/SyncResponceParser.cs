using BitwardenApi.Modules.Vault.Abstractions;
using BitwardenApi.Modules.Vault.Models;
using Wololo.Text.Json;

namespace BitwardenApi.Modules.Vault.SyncParser;

public sealed partial class SyncResponseParser : IDisposable
{
    private const int BufferSize = 1024 * 16;

    private readonly ISyncDataWriter _dataWriter;
    private readonly CipherPayloadCapture _cipherPayloadCapture;
    private readonly Utf8JsonStreamReader _reader;
    private ObjectCaptureState _objectCaptureState;

    private ArrayCaptureState _arrayCaptureState;
    private RootProperty _pendingRootProperty;

    private CipherDto _cipherDto;
    private CipherProperty _cipherProperty;
    private int _parsedCiphers;

    private CollectionDto _collectionDto;
    private CollectionProperty _collectionProperty;
    private int _parsedCollections;

    private FolderDto _folderDto;
    private FolderProperty _folderProperty;
    private int _parsedFolders;

    public SyncResponseParser(ISyncDataWriter dataWriter)
    {
        _dataWriter = dataWriter;
        _cipherPayloadCapture = new CipherPayloadCapture();
        _reader = new Utf8JsonStreamReader(BufferSize);
    }

    public static async Task<SyncParserReport> ParseAsync(ISyncDataWriter dataWriter, Stream stream, CancellationToken token)
    {
        using var parser = new SyncResponseParser(dataWriter);
        return await parser.ParseAsyncCore(stream, token);
    }

    public void Dispose()
    {
        _cipherPayloadCapture.Dispose();
        _reader.Dispose();
    }

    private async Task<SyncParserReport> ParseAsyncCore(Stream stream, CancellationToken token)
    {
        await _reader.ReadAsync(stream, OnRead, token);

        if (_pendingRootProperty != RootProperty.None || _arrayCaptureState.IsActive || _objectCaptureState.IsActive)
        {
            throw new InvalidDataException("Sync response JSON ended before the parser completed.");
        }

        return new SyncParserReport(_parsedCiphers, _parsedFolders, _parsedCollections);
    }

    private void OnRead(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.PropertyName && reader.CurrentDepth == 1)
        {
            _pendingRootProperty = MatchRootProperty(ref reader);
            return;
        }

        switch (_pendingRootProperty)
        {
            case RootProperty.Folders:
                CaptureArray(
                    ref reader,
                    ref _folderDto,
                    ref _folderProperty,
                    ParseFolder,
                    static (writer, ref dto) =>
                    {
                        EnsureFolderIsComplete(ref dto);
                        writer.WriteFolder(ref dto);
                    });
                _parsedFolders = Math.Max(_parsedFolders, _arrayCaptureState.ProcessedItems);
                ClearPendingRootPropertyIfArrayCompleted();
                return;
            case RootProperty.Collections:
                CaptureArray(
                    ref reader,
                    ref _collectionDto,
                    ref _collectionProperty,
                    ParseCollection,
                    static (writer, ref dto) =>
                    {
                        EnsureCollectionIsComplete(ref dto);
                        writer.WriteCollection(ref dto);
                    });
                _parsedCollections = Math.Max(_parsedCollections, _arrayCaptureState.ProcessedItems);
                ClearPendingRootPropertyIfArrayCompleted();
                return;
            case RootProperty.Ciphers:
                CaptureArray(
                    ref reader,
                    ref _cipherDto,
                    ref _cipherProperty,
                    ParseCipher,
                    (writer, ref dto) =>
                    {
                        EnsureCipherIsComplete(ref dto);
                        writer.WriteCipher(ref dto, _cipherPayloadCapture.PayloadSpan);
                    });
                _parsedCiphers = Math.Max(_parsedCiphers, _arrayCaptureState.ProcessedItems);
                ClearPendingRootPropertyIfArrayCompleted();
                return;
            case RootProperty.Ignore:
                _pendingRootProperty = RootProperty.None;
                reader.TrySkip();
                return;
            default:
                return;
        }
    }

    private void ClearPendingRootPropertyIfArrayCompleted()
    {
        if (!_arrayCaptureState.IsActive && !_objectCaptureState.IsActive)
        {
            _pendingRootProperty = RootProperty.None;
        }
    }

    private static RootProperty MatchRootProperty(ref Utf8JsonReader reader)
    {
        if (reader.ValueTextEquals("folders"u8) || reader.ValueTextEquals("Folders"u8))
        {
            return RootProperty.Folders;
        }

        if (reader.ValueTextEquals("collections"u8) || reader.ValueTextEquals("Collections"u8))
        {
            return RootProperty.Collections;
        }

        if (reader.ValueTextEquals("ciphers"u8) || reader.ValueTextEquals("Ciphers"u8))
        {
            return RootProperty.Ciphers;
        }

        return RootProperty.Ignore;
    }

    private enum RootProperty
    {
        None = 0,
        Folders,
        Collections,
        Ciphers,
        Ignore
    }
}

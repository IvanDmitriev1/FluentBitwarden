using System.Text.Json;
using BitwardenApi.Models;
using FluentBitwarden.Modules.Vault.Abstractions;
using Wololo.Text.Json;

namespace FluentBitwarden.Modules.Vault.Internal.SyncParser;

[Fody.ConfigureAwait(false)]
internal sealed partial class VaultSyncResponseParser(IVaultWriterRepository dataWriter) : IDisposable
{
    private const int BufferSize = 1024 * 16;

    private readonly CipherPayloadCapture _cipherPayloadCapture = new();
    private readonly Utf8JsonStreamReader _reader = new(BufferSize);
    private ObjectCaptureState _objectCaptureState;

    private ArrayCaptureState _arrayCaptureState;
    private RootProperty _pendingRootProperty;

    private VaultCipherDto _vaultCipherDto;
    private CipherProperty _cipherProperty;
    private int _parsedCiphers;

    private VaultCollectionDto _vaultCollectionDto;
    private CollectionProperty _collectionProperty;
    private int _parsedCollections;

    private VaultFolderDto _vaultFolderDto;
    private FolderProperty _folderProperty;
    private int _parsedFolders;

    public static async Task<SyncParserReport> ParseAsync(IVaultWriterRepository dataWriter, Stream stream, CancellationToken token)
    {
        using var parser = new VaultSyncResponseParser(dataWriter);
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
                    ref _vaultFolderDto,
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
                    ref _vaultCollectionDto,
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
                    ref _vaultCipherDto,
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

using System.Buffers;
using BitwardenApi.Modules.Vault.Abstractions;
using BitwardenApi.Modules.Vault.Models;
using CommunityToolkit.HighPerformance.Buffers;
using Wololo.Text.Json;

namespace BitwardenApi.Modules.Vault.SyncParser;

public sealed partial class SyncResponceParser : IDisposable
{
    public SyncResponceParser(ISyncDataWriter dataWriter)
    {
        _dataWriter = dataWriter;
        _reader = new Utf8JsonStreamReader(BufferSize);

        _captureBuffer = new ArrayPoolBufferWriter<byte>(ArrayPool<byte>.Shared, 1024);
        _captureWriter = new Utf8JsonWriter(_captureBuffer);
    }

    private const int BufferSize = 1024 * 16;

    private readonly ISyncDataWriter _dataWriter;
    private readonly Utf8JsonStreamReader _reader;
    private readonly ArrayPoolBufferWriter<byte> _captureBuffer;
    private readonly Utf8JsonWriter _captureWriter;

    private RootProperty _pendingRootProperty;
    private ArrayCaptureState _arrayCaptureState;
    private ObjectCaptureState _objectCaptureState;

    private CipherState _cipherState;
    private int _parsedCiphers;

    private CollectionState _collectionState;
    private int _parsedCollections;

    private FolderState _folderState;
    private int _parsedFolders;

    public static async Task<SyncParserReport> ParseAsync(ISyncDataWriter dataWriter, Stream stream, CancellationToken token)
    {
        using var parser = new SyncResponceParser(dataWriter);
        return await parser.ParseAsyncCore(stream, token);
    }

    public void Dispose()
    {
        _captureWriter.Dispose();
        _captureBuffer.Dispose();
        _reader.Dispose();
    }

    private async Task<SyncParserReport> ParseAsyncCore(Stream stream, CancellationToken token)
    {
        await _reader.ReadAsync(stream, OnRead, token);
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
            {
                CaptureArray(
                    ref reader,
                    ref _folderState,
                    ParseFolder,
                    static (writer, ref state, payload) => writer.WriteFolder(new FolderDto()
                    {
                        Id = state.Id!.Value,
                        Payload = payload,
                    }));
                _parsedFolders = int.Max(_parsedFolders, _arrayCaptureState.ProcessedItems);
                break;
            }
            case RootProperty.Collections:
            {
                CaptureArray(
                    ref reader,
                    ref _collectionState,
                    ParseCollection,
                    static (writer, ref state, payload) => writer.WriteCollection(new CollectionDto()
                    {
                        Id = state.Id!.Value,
                        Payload = payload,
                    }));
                _parsedCollections = int.Max(_parsedCollections, _arrayCaptureState.ProcessedItems);
                break;
            }
            case RootProperty.Ciphers:
            {
                CaptureArray(
                    ref reader,
                    ref _cipherState,
                    ParseCipher,
                    static (writer, ref state, payload) => writer.WriteCipher(new CipherDto()
                    {
                        Id = state.Id!.Value,
                        FolderId = state.FolderId,
                        CipherType = state.Type!.Value,
                        Payload = payload,
                    }));
                _parsedCiphers = int.Max(_parsedCiphers, _arrayCaptureState.ProcessedItems);
                break;
            }
            default:
                reader.TrySkip();
                break;
        }
    }

    private static RootProperty MatchRootProperty(ref Utf8JsonReader reader)
    {
        if (reader.ValueTextEquals("profile"u8))
        {
            return RootProperty.Profile;
        }

        if (reader.ValueTextEquals("userDecryption"u8))
        {
            return RootProperty.UserDecryption;
        }

        if (reader.ValueTextEquals("folders"u8))
        {
            return RootProperty.Folders;
        }

        if (reader.ValueTextEquals("collections"u8))
        {
            return RootProperty.Collections;
        }

        if (reader.ValueTextEquals("ciphers"u8))
        {
            return RootProperty.Ciphers;
        }

        return RootProperty.Ignore;
    }

    private enum RootProperty
    {
        None = 0,
        Profile,
        UserDecryption,
        Folders,
        Collections,
        Ciphers,
        Ignore
    }
}
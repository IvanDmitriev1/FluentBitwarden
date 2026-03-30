using CommunityToolkit.HighPerformance.Buffers;
using FluentBitwarden.Modules.Security.Abstractions;
using FluentBitwarden.Shared.Extensions;
using System.Security.Cryptography;

namespace FluentBitwarden.Modules.Security.Services;

internal sealed class DpapiSecretProtector : ISecretProtector
{
    private static ReadOnlySpan<byte> Entropy => "bw_session_v1"u8;

    public void Protect(string filePath, ReadOnlySpan<byte> plaintext)
    {
        using var protectedPayloadOwner = ProtectPayload(plaintext, out var protectedPayloadLength);
        FilePathHelpers.EnsureParentDirectoryExists(filePath);

        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 128);
        stream.Write(protectedPayloadOwner.Span[..protectedPayloadLength]);
    }

    public byte[]? TryUnprotect(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        using var protectedPayloadOwner = FilePathHelpers.ReadAllBytesOwner(filePath);
        using var plaintextOwner = MemoryOwner<byte>.Allocate(protectedPayloadOwner.Length);

        try
        {
            if (!ProtectedData.TryUnprotect(
                    protectedPayloadOwner.Span,
                    DataProtectionScope.CurrentUser,
                    plaintextOwner.Span,
                    out var bytesWritten,
                    Entropy))
            {
                return null;
            }

            return plaintextOwner.Span[..bytesWritten].ToArray();
        }
        catch (CryptographicException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(protectedPayloadOwner.Span);
            CryptographicOperations.ZeroMemory(plaintextOwner.Span);
        }
    }

    private static MemoryOwner<byte> ProtectPayload(ReadOnlySpan<byte> plaintext, out int bytesWritten)
    {
        var protectedPayloadOwner = MemoryOwner<byte>.Allocate(1536);

        return ProtectedData.TryProtect(
            plaintext,
            DataProtectionScope.CurrentUser,
            protectedPayloadOwner.Span,
            out bytesWritten,
            Entropy)
            ? protectedPayloadOwner
            : throw new CryptographicException("DPAPI protection failed.");
    }
}

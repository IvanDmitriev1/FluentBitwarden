using FluentBitwarden.Modules.Passkey.Internal;
using System.Text.Json.Serialization;

namespace FluentBitwarden.Modules.Passkey.Models;

internal readonly record struct PasskeyGetAssertionRequest(
    string RpId,
    [property: JsonConverter(typeof(Base64UrlByteArrayJsonConverter))]
    byte[] RpIdHash,
    [property: JsonConverter(typeof(Base64UrlByteArrayJsonConverter))]
    byte[] ClientDataHash);

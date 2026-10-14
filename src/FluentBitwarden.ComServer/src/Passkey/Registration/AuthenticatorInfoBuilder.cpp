#include "pch.h"
#include "Passkey/Registration/AuthenticatorInfoBuilder.h"
#include "Infrastructure/Cbor/CborWriter.h"

namespace FluentBitwarden::ComServer::PasskeyPlugin::Registration
{
    std::vector<uint8_t> BuildAuthenticatorGetInfoCbor()
    {
        constexpr std::array<std::uint8_t, 16> aaguid =
        {
            0x5a, 0x97, 0xbd, 0x16,
            0xf0, 0xce,
            0x4c, 0xb3,
            0x96, 0xf3,
            0x4f, 0xd2, 0x0f, 0x86, 0xea, 0x83
        };

        constexpr std::array<std::string_view, 2> versions =
        {
            "FIDO_2_0",
            "FIDO_2_1"
        };

        constexpr std::array<std::string_view, 2> extensions =
        {
            "prf",
            "hmac-secret"
        };

        constexpr std::array<std::string_view, 1> transports =
        {
            "internal"
        };

        constexpr std::array<AuthenticatorOption, 3> options =
        {
            AuthenticatorOption{ "rk", true },
            AuthenticatorOption{ "up", true },
            AuthenticatorOption{ "uv", true }
        };

        CborWriter writer;
        writer.WriteMap(6);

        writer.WriteKey(0x01);
        writer.WriteTextArray(std::span{ versions });

        writer.WriteKey(0x02);
        writer.WriteTextArray(std::span{ extensions });

        writer.WriteKey(0x03);
        writer.WriteBytes(std::span{ aaguid });

        writer.WriteKey(0x04);
        writer.WriteMap(options.size());
        for (const auto& option : options)
        {
            writer.WriteText(option.name);
            writer.WriteBool(option.value);
        }

        writer.WriteKey(0x09);
        writer.WriteTextArray(std::span{ transports });

        writer.WriteKey(0x0A);
        writer.WriteArrayHeader(1);
        writer.WriteMap(2);
        writer.WriteText("alg");
        writer.WriteInteger(-7);
        writer.WriteText("type");
        writer.WriteText("public-key");

        return writer.Finish();
    }
}

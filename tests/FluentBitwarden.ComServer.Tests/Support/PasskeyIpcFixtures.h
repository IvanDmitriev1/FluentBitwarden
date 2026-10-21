#pragma once

#include "Passkey/Models/PasskeyAssertionMessage.h"
#include "Passkey/Models/PasskeyMakeCredentialMessage.h"

namespace FluentBitwarden::ComServer::Tests::PasskeyIpcFixtures
{
    inline WebAuthn::PasskeyGetAssertionRequest GetAssertionRequest()
    {
        return {
            .RpId = "rp",
            .RpIdHash = std::vector<std::uint8_t>(32, 0x11),
            .ClientDataHash = std::vector<std::uint8_t>(32, 0x22)
        };
    }

    inline std::vector<std::byte> GetAssertionRequestPayload()
    {
        std::vector<std::byte> expected{
            std::byte{ 0x03 },
            std::byte{ 0xfd }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff },
            std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff },
            std::byte{ 0x72 }, std::byte{ 0x70 },
            std::byte{ 0x20 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }
        };
        expected.insert(expected.end(), 32, std::byte{ 0x11 });
        expected.insert(expected.end(), {
            std::byte{ 0x20 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }
        });
        expected.insert(expected.end(), 32, std::byte{ 0x22 });
        return expected;
    }

    inline std::vector<std::byte> GetAssertionResponsePayload()
    {
        return {
            std::byte{ 0x01 }, std::byte{ 0x06 },
            std::byte{ 0x01 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0xaa },
            std::byte{ 0x02 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0xbb }, std::byte{ 0xcc },
            std::byte{ 0x03 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x11 }, std::byte{ 0x22 }, std::byte{ 0x33 },
            std::byte{ 0x01 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0xdd },
            std::byte{ 0xfe }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff },
            std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0x75 },
            std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }
        };
    }

    inline WebAuthn::PasskeyMakeCredentialRequest MakeCredentialRequest()
    {
        return {
            .RpId = "r",
            .RpName = "n",
            .RpIdHash = std::vector<std::uint8_t>(32, 0x11),
            .ClientDataHash = std::vector<std::uint8_t>(32, 0x22),
            .UserId = { 0xab, 0x00 },
            .UserName = "u",
            .UserDisplayName = "d",
            .RequireResidentKey = true,
            .UserVerification = false
        };
    }

    inline std::vector<std::byte> MakeCredentialRequestPayload()
    {
        std::vector<std::byte> expected{
            std::byte{ 0x09 },
            std::byte{ 0xfe }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff },
            std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0x72 },
            std::byte{ 0xfe }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff },
            std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0x6e },
            std::byte{ 0x20 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }
        };
        expected.insert(expected.end(), 32, std::byte{ 0x11 });
        expected.insert(expected.end(), {
            std::byte{ 0x20 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }
        });
        expected.insert(expected.end(), 32, std::byte{ 0x22 });
        expected.insert(expected.end(), {
            std::byte{ 0x02 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 },
            std::byte{ 0xab }, std::byte{ 0x00 },
            std::byte{ 0xfe }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff },
            std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0x75 },
            std::byte{ 0xfe }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff },
            std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0x64 },
            std::byte{ 0x01 }, std::byte{ 0x00 }
        });
        return expected;
    }

    inline std::vector<std::byte> MakeCredentialResponsePayload()
    {
        return {
            std::byte{ 0x01 }, std::byte{ 0x01 },
            std::byte{ 0x02 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 },
            std::byte{ 0x44 }, std::byte{ 0x55 }
        };
    }
}

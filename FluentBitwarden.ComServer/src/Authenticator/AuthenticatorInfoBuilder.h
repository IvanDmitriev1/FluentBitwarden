#pragma once
#include <array>
#include <cstdint>
#include <string_view>
#include <vector>

namespace FluentBitwarden::PasskeyPlugin::Registration
{
    struct AuthenticatorOption
    {
        std::string_view name;
        bool value;
    };

    struct AuthenticatorInfoConfig
    {
        std::vector<std::string_view> versions;
        std::array<uint8_t, 16> aaguid{};
        std::vector<AuthenticatorOption> options;
    };

    std::vector<uint8_t> BuildAuthenticatorGetInfoCbor();
}
#pragma once
#include <pch.h>

namespace FluentBitwarden::ComServer::WebAuthn::Payload
{
    inline constexpr size_t Sha256HashLength = 32;

    inline void ValidateSha256Hash(std::span<const std::uint8_t> value, std::string_view name)
    {
        if (value.size() != Sha256HashLength)
        {
            throw std::runtime_error(std::string{ name } + " must be 32 bytes.");
        }
    }
}

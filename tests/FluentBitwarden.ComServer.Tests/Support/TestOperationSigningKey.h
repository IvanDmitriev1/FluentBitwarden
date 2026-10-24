#pragma once

#include <cstdint>
#include <span>
#include <vector>

namespace FluentBitwarden::ComServer::Tests::Support
{
    class TestOperationSigningKey final
    {
    public:
        [[nodiscard]] static const std::vector<std::uint8_t>& PublicKeyBlob();
        [[nodiscard]] static std::vector<std::uint8_t> Sign(
            std::span<const std::uint8_t> request);
    };
}

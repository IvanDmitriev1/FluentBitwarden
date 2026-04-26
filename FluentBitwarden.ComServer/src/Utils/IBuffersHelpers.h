#pragma once
#include <pch.h>

namespace FluentBitwarden::ComServer::Utils::IBuffersHelpers
{
    [[nodiscard]] IBuffer Allocate(std::size_t length);
    [[nodiscard]] IBuffer CopyFrom(std::span<const std::byte> bytes);

    [[nodiscard]] std::span<std::byte> AsWritableBytes(const IBuffer& buffer);
    [[nodiscard]] std::span<const std::byte> AsBytes(const IBuffer& buffer);

    [[nodiscard]] std::vector<std::byte> ToVector(const IBuffer& buffer);
}
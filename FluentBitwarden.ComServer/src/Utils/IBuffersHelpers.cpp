#include "pch.h"
#include "IBuffersHelpers.h"

namespace Streams = winrt::Windows::Storage::Streams;

namespace FluentBitwarden::ComServer::Utils::IBuffersHelpers
{
	IBuffer Allocate(std::size_t length)
	{
        Streams::Buffer buffer{ static_cast<std::uint32_t>(length) };

        // We are going to write directly into the backing memory.
        // Length means "bytes currently in use".
        buffer.Length(static_cast<std::uint32_t>(length));

        return buffer;
	}

    IBuffer CopyFrom(std::span<const std::byte> bytes)
    {
        auto buffer = Allocate(bytes.size());

        if (!bytes.empty())
        {
            auto target = AsWritableBytes(buffer);
            std::memcpy(target.data(), bytes.data(), bytes.size());
        }

        return buffer;
    }

    std::span<std::byte> AsWritableBytes(const IBuffer& buffer)
    {
        auto* data = reinterpret_cast<std::byte*>(buffer.data());
        return { data, buffer.Length() };
    }

    std::span<const std::byte> AsBytes(const IBuffer& buffer)
    {
        auto* data = reinterpret_cast<const std::byte*>(buffer.data());
        return { data, buffer.Length() };

    }

    std::vector<std::byte> ToVector(const IBuffer& buffer)
    {
        auto bytes = AsBytes(buffer);
        return { bytes.begin(), bytes.end() };
    }


}
#pragma once
#include <concepts>
#include <span>
#include <cstring>
#include <stdexcept>

namespace FluentBitwarden::ComServer::Ipc::Binary
{
	static_assert(std::endian::native == std::endian::little,
				  "This IPC code assumes a little-endian Windows target.");

	template <std::unsigned_integral T>
	[[nodiscard]] T ReadLe(std::span<const std::byte> bytes)
	{
		if (bytes.size() != sizeof(T))
		{
			throw std::invalid_argument("Invalid little-endian integer size.");
		}

		T value{};
		std::memcpy(&value, bytes.data(), sizeof(T));
		return value;
	}

	template <std::unsigned_integral T>
	void WriteLe(std::span<std::byte> bytes, T value)
	{
		if (bytes.size() != sizeof(T))
		{
			throw std::invalid_argument("Invalid little-endian integer size.");
		}

		std::memcpy(bytes.data(), &value, sizeof(T));
	}
}
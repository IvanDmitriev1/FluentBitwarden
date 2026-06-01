#pragma once
#include <array>
#include <cstddef>
#include <cstdint>
#include <bit>
#include <concepts>
#include <span>
#include <string_view>
#include <vector>

namespace FluentBitwarden::ComServer::Ipc
{
	namespace Constants
	{
		inline constexpr std::wstring_view PipePath = LR"(\\.\pipe\LOCAL\FluentBitwarden.v2)";
		inline constexpr std::uint16_t ProtocolVersion = 2;
	}

	struct RequestHeader
	{
		std::uint16_t MessageType{};
		std::int32_t PayloadLength{};

		static constexpr std::size_t Size = 8;
		static constexpr std::size_t VersionOffset = 0;
		static constexpr std::size_t MessageTypeOffset = VersionOffset + sizeof(std::uint16_t);
		static constexpr std::size_t PayloadLengthOffset = MessageTypeOffset + sizeof(std::uint16_t);

		static_assert(PayloadLengthOffset + sizeof(std::int32_t) == Size);
		static_assert(sizeof(std::uint16_t) == 2);
		static_assert(sizeof(std::int32_t) == 4);

		[[nodiscard]] static RequestHeader Parse(std::span<const std::byte> bytes);
		[[nodiscard]] std::array<std::byte, Size> Write() const;
	};

	struct ResponseHeader
	{
		std::int32_t PayloadLength{};

		static constexpr std::size_t Size = sizeof(std::uint16_t) + sizeof(std::int32_t);
		static constexpr std::size_t VersionOffset = 0;
		static constexpr std::size_t PayloadLengthOffset = VersionOffset + sizeof(std::uint16_t);

		static_assert(PayloadLengthOffset + sizeof(std::int32_t) == Size);
		static_assert(sizeof(std::uint16_t) == 2);
		static_assert(sizeof(std::int32_t) == 4);

		[[nodiscard]] static ResponseHeader Parse(std::span<const std::byte> bytes);
	};

	template <typename T>
	concept IpcBinaryRequest =
		std::movable<T> &&
		requires(const T& value)
	{
		{ T::MessageType } -> std::convertible_to<std::uint16_t>;
		{ value.ToPayload() } -> std::same_as<std::vector<std::byte>>;
	};

	template <typename T>
	concept IpcBinaryResponse =
		std::movable<T> &&
		requires(std::span<const std::byte> payload)
	{
		{ T::FromPayload(payload) } -> std::same_as<T>;
	};
}

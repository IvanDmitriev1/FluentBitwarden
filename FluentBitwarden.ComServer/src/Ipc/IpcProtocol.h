#pragma once
#include <array>
#include <cstddef>
#include <cstdint>
#include <bit>

namespace FluentBitwarden::ComServer::Ipc
{
	namespace Constants
	{
		inline constexpr std::wstring_view PipePath = LR"(\\.\pipe\LOCAL\FluentBitwarden.v1)";
		inline constexpr std::uint16_t ProtocolVersion = 1;
		inline constexpr std::int32_t MaxPayloadLength = 1024 * 1024;
	}

	struct PipeHeader
	{
		std::uint16_t MessageType{};
		std::uint32_t PayloadLength{};

		static constexpr std::size_t Size = 8;
		static constexpr std::size_t VersionOffset = 0;
		static constexpr std::size_t MessageTypeOffset = VersionOffset + sizeof(std::uint16_t);
		static constexpr std::size_t PayloadLengthOffset = MessageTypeOffset + sizeof(std::uint16_t);

		static_assert(PayloadLengthOffset + sizeof(std::uint32_t) == Size);
		static_assert(sizeof(std::uint16_t) == 2);
		static_assert(sizeof(std::uint32_t) == 4);

		[[nodiscard]] static PipeHeader Parse(std::span<const std::byte> bytes);
		[[nodiscard]] std::array<std::byte, Size> Write() const;
	};

	template <typename T>
	concept IpcJsonRequest =
		std::movable<T> &&
		requires(const T& value)
	{
		{ T::MessageType } -> std::convertible_to<std::uint16_t>;
		{ value.ToJson() } -> std::same_as<JsonObject>;
	};

	template <typename T>
	concept IpcJsonResponse =
		std::movable<T> &&
		requires(JsonObject json)
	{
		{ T::FromJson(json) } -> std::same_as<T>;
	};
}
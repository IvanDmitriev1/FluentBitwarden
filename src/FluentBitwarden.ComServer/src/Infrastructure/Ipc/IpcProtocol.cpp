#include <pch.h>
#include "Infrastructure/Ipc/IpcProtocol.h"
#include "Infrastructure/Ipc/IpcBinary.h"

namespace FluentBitwarden::ComServer::Ipc
{
	RequestHeader RequestHeader::Parse(std::span<const std::byte> bytes)
	{
		if (bytes.size() != Size)
		{
			throw std::invalid_argument("Invalid IPC header size.");
		}

		const auto version = Binary::ReadLe<std::uint16_t>(
			bytes.subspan(VersionOffset, sizeof(std::uint16_t)));

		if (version != Constants::ProtocolVersion)
		{
			throw std::runtime_error("Incompatible IPC protocol version.");
		}

		const auto messageType = Binary::ReadLe<std::uint16_t>(
			bytes.subspan(MessageTypeOffset, sizeof(std::uint16_t)));

		const auto payloadLength = Binary::ReadLe<std::int32_t>(
			bytes.subspan(PayloadLengthOffset, sizeof(std::int32_t)));


		if (payloadLength < 0)
		{
			throw std::runtime_error("Invalid IPC payload length.");
		}

		return RequestHeader
		{
			.MessageType = messageType,
			.PayloadLength = payloadLength
		};
	}

	std::array<std::byte, RequestHeader::Size> RequestHeader::Write() const
	{
		if (PayloadLength < 0)
		{
			throw std::runtime_error("Invalid IPC payload length.");
		}

		std::array<std::byte, Size> bytes{};
		auto span = std::span<std::byte>{ bytes };

		Binary::WriteLe<std::uint16_t>(
			span.subspan(VersionOffset, sizeof(std::uint16_t)),
			Constants::ProtocolVersion);

		Binary::WriteLe<std::uint16_t>(
			span.subspan(MessageTypeOffset, sizeof(std::uint16_t)),
			MessageType);

		Binary::WriteLe<std::int32_t>(
			span.subspan(PayloadLengthOffset, sizeof(std::int32_t)),
			PayloadLength);

		return bytes;
	}

	ResponseHeader ResponseHeader::Parse(std::span<const std::byte> bytes)
	{
		if (bytes.size() != Size)
		{
			throw std::invalid_argument("Invalid IPC header size.");
		}

		const auto version = Binary::ReadLe<std::uint16_t>(
			bytes.subspan(VersionOffset, sizeof(std::uint16_t)));

		if (version != Constants::ProtocolVersion)
		{
			throw std::runtime_error("Incompatible IPC protocol version.");
		}

		const auto isSuccessful = Binary::ReadLe<std::uint8_t>(
			bytes.subspan(IsSuccessfulOffset, sizeof(std::uint8_t)));

		if (isSuccessful > 1)
		{
			throw std::runtime_error("Invalid IPC response success flag.");
		}

		const auto payloadLength = Binary::ReadLe<std::int32_t>(
			bytes.subspan(PayloadLengthOffset, sizeof(std::int32_t)));

		if (payloadLength < 0)
		{
			throw std::runtime_error("Invalid IPC payload length.");
		}

		if (isSuccessful == 0 && payloadLength != 0)
		{
			throw std::runtime_error("Failed IPC response must not include a payload.");
		}

		return ResponseHeader
		{
			.IsSuccessful = isSuccessful != 0,
			.PayloadLength = payloadLength
		};
	}
}

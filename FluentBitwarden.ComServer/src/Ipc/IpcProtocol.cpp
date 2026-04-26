#include <pch.h>
#include "IpcProtocol.h"
#include "Internal/IpcBinary.h"

namespace FluentBitwarden::ComServer::Ipc
{
	PipeHeader PipeHeader::Parse(std::span<const std::byte> bytes)
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

		const auto payloadLength = Binary::ReadLe<std::uint32_t>(
			bytes.subspan(PayloadLengthOffset, sizeof(std::uint32_t)));


		if (payloadLength > Constants::MaxPayloadLength)
		{
			throw std::runtime_error("Invalid IPC payload length.");
		}

		return PipeHeader
		{
			.MessageType = messageType,
			.PayloadLength = payloadLength
		};
	}

	std::array<std::byte, PipeHeader::Size> PipeHeader::Write() const
	{
		if (PayloadLength > Constants::MaxPayloadLength)
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

		Binary::WriteLe<std::uint32_t>(
			span.subspan(PayloadLengthOffset, sizeof(std::uint32_t)),
			PayloadLength);

		return bytes;
	}
}
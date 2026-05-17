#include "pch.h"
#include "NamedPipeClient.h"
#include "PipeWin32.h"
#include "IpcProtocol.h"

namespace FluentBitwarden::ComServer::Ipc
{
	NamedPipeClient::NamedPipeClient(std::wstring_view pipeName)
		:m_pipePath(std::wstring(pipeName))
	{
	}

	wil::task<std::vector<std::byte>> NamedPipeClient::SendBinaryRequestAsync(uint16_t requestType, std::vector<std::byte> payload)
	{
		if (!m_pipe)
			m_pipe = PipeWin32::OpenOverlappedPipe(m_pipePath, ConnectTimeout);

		co_await WritePayload(requestType, std::move(payload));
		co_return co_await ReadResponsePayloadAsync();
	}

	wil::task<void> NamedPipeClient::WritePayload(uint16_t messageType, std::vector<std::byte> payload)
	{
		THROW_HR_IF(E_INVALIDARG, payload.size() > Constants::MaxPayloadLength);

		RequestHeader header
		{
			.MessageType = messageType,
			.PayloadLength =  static_cast<std::uint32_t>(payload.size())
		};

		const auto headerBytes = header.Write();

		co_await PipeWin32::WriteExactly(m_pipe.get(), headerBytes);
		co_await PipeWin32::WriteExactly(
			m_pipe.get(),
			std::span<const std::byte>{ payload.data(), payload.size() });
		co_return;
	}

	wil::task<std::vector<std::byte>> NamedPipeClient::ReadResponsePayloadAsync()
	{
		const auto responseHeaderBuffer = co_await PipeWin32::ReadExactly(m_pipe.get(), ResponseHeader::Size);
		const auto responseHeader = ResponseHeader::Parse(
			std::span<const std::byte>{ responseHeaderBuffer.data(), responseHeaderBuffer.size() });

		co_return co_await PipeWin32::ReadExactly(m_pipe.get(), responseHeader.PayloadLength);
	}
}

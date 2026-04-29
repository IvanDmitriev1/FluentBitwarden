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

	wil::task<JsonObject> NamedPipeClient::SendJsonRequestAsync(uint16_t requestType, JsonObject json)
	{
		if (!m_pipe)
			m_pipe = PipeWin32::OpenOverlappedPipe(m_pipePath, ConnectTimeout);
		
		const std::string utf8 = winrt::to_string(json.Stringify());

		co_await WritePayload(requestType, std::move(utf8));
		co_return co_await ReadJsonResponseAsync();
	}

	wil::task<void> NamedPipeClient::WritePayload(uint16_t messageType, std::string utf8)
	{
		const auto payload = std::as_bytes(std::span{ utf8.data(), utf8.size() });

		THROW_HR_IF(E_INVALIDARG, payload.size() > Constants::MaxPayloadLength);

		RequestHeader header
		{
			.MessageType = messageType,
			.PayloadLength =  static_cast<std::uint32_t>(payload.size())
		};

		const auto headerBytes = header.Write();

		co_await PipeWin32::WriteExactly(m_pipe.get(), headerBytes);
		co_await PipeWin32::WriteExactly(m_pipe.get(), payload);
		co_return;
	}

	wil::task<JsonObject> NamedPipeClient::ReadJsonResponseAsync()
	{
		const auto responseHeaderBuffer = co_await PipeWin32::ReadExactly(m_pipe.get(), ResponseHeader::Size);
		const auto responseHeader = ResponseHeader::Parse(responseHeaderBuffer);

		const auto responsePayload = co_await PipeWin32::ReadExactly(m_pipe.get(), responseHeader.PayloadLength);

		const std::string_view utf8
		{
			reinterpret_cast<const char*>(responsePayload.data()),
			responsePayload.size()
		};

		co_return JsonObject::Parse(winrt::to_hstring(utf8));
	}
}

#include "pch.h"
#include "NamedPipeClient.h"
#include "Internal/PipeWin32.h"
#include "IpcProtocol.h"
#include "Utils/IBuffersHelpers.h"

namespace FluentBitwarden::ComServer::Ipc
{
	NamedPipeClient::NamedPipeClient(std::wstring_view pipeName)
		:m_pipePath(std::wstring(pipeName))
	{
	}

	IAsyncOperation<JsonObject> NamedPipeClient::SendJsonRequestAsync(uint16_t requestType, JsonObject request)
	{
		co_await winrt::resume_background();

		if (!m_pipe)
			m_pipe = PipeWin32::OpenOverlappedPipe(m_pipePath, ConnectTimeout);
		
		co_await WritePayload(requestType, request);
		auto result = co_await ReadJsonResponseAsync();
		co_return result;
	}

	IAsyncAction NamedPipeClient::WritePayload(uint16_t messageType, JsonObject json)
	{
		const std::string utf8 = winrt::to_string(json.Stringify());
		const auto payload = std::as_bytes(std::span{ utf8.data(), utf8.size() });

		THROW_HR_IF(E_INVALIDARG, payload.size() > Constants::MaxPayloadLength);

		PipeHeader header
		{
			.MessageType = messageType,
			.PayloadLength =  static_cast<std::uint32_t>(payload.size())
		};

		const auto headerBytes = header.Write();

		co_await PipeWin32::WriteExactly(m_pipe.get(), headerBytes);
		co_await PipeWin32::WriteExactly(m_pipe.get(), payload);
	}

	IAsyncOperation<JsonObject> NamedPipeClient::ReadJsonResponseAsync()
	{
		const auto responseHeaderBuffer = co_await PipeWin32::ReadExactly(m_pipe.get(), PipeHeader::Size);
		const auto responseHeader = PipeHeader::Parse(Utils::IBuffersHelpers::AsBytes(responseHeaderBuffer));

		const auto responsePayload = co_await PipeWin32::ReadExactly(m_pipe.get(), responseHeader.PayloadLength);
		const auto bytes = Utils::IBuffersHelpers::AsBytes(responsePayload);

		const std::string_view utf8
		{
			reinterpret_cast<const char*>(bytes.data()),
			bytes.size()
		};

		co_return JsonObject::Parse(winrt::to_hstring(utf8));
	}
}
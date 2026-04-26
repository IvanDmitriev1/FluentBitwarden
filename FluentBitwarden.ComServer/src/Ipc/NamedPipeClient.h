#pragma once
#include <pch.h>
#include "IpcProtocol.h"

using namespace std::chrono_literals;

namespace FluentBitwarden::ComServer::Ipc
{
	class NamedPipeClient final
	{
		std::chrono::milliseconds ConnectTimeout = 2s;

	public:
		NamedPipeClient(std::wstring_view pipeName);
		~NamedPipeClient() = default;

		NamedPipeClient(NamedPipeClient const&) = delete;
		NamedPipeClient& operator=(NamedPipeClient const&) = delete;

		NamedPipeClient(NamedPipeClient&&) noexcept = default;
		NamedPipeClient& operator=(NamedPipeClient&&) noexcept = default;

	public:
		template <IpcJsonRequest TRequest, IpcJsonResponse TResponse>
		[[nodiscard]] wil::task<TResponse> SendAsync(TRequest request);

	private:
		IAsyncOperation<JsonObject> SendJsonRequestAsync(uint16_t messageType, JsonObject request);

		IAsyncAction WritePayload(uint16_t messageType, JsonObject json);
		IAsyncOperation<JsonObject> ReadJsonResponseAsync();

	private:
		wil::unique_hfile m_pipe;
		std::wstring m_pipePath;
	};

	template<IpcJsonRequest TRequest, IpcJsonResponse TResponse>
	inline wil::task<TResponse> NamedPipeClient::SendAsync(TRequest request)
	{
		JsonObject requestJson = request.ToJson();

		JsonObject responseJson =
			co_await SendJsonRequestAsync(
			TRequest::MessageType,
			requestJson);

		co_return TResponse::FromJson(responseJson);
	}
}

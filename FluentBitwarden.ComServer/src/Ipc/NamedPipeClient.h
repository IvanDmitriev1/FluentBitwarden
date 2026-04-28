#pragma once
#include <pch.h>
#include "IpcProtocol.h"
#include "IpcResult.h"

using namespace std::chrono_literals;

namespace FluentBitwarden::ComServer::Ipc
{
	class NamedPipeClient final
	{
		std::chrono::milliseconds ConnectTimeout = 5s;

	public:
		NamedPipeClient(std::wstring_view pipeName);
		~NamedPipeClient() = default;

		NamedPipeClient(NamedPipeClient const&) = delete;
		NamedPipeClient& operator=(NamedPipeClient const&) = delete;

		NamedPipeClient(NamedPipeClient&&) noexcept = default;
		NamedPipeClient& operator=(NamedPipeClient&&) noexcept = default;

	public:
		template <IpcJsonRequest TRequest, IpcJsonResponse TResponse>
		[[nodiscard]] wil::task<IpcResult<TResponse>> SendAsync(TRequest request);

	private:
		wil::task<JsonObject> SendJsonRequestAsync(uint16_t messageType, JsonObject json);

		wil::task<void> WritePayload(uint16_t messageType, std::string utf8);
		wil::task<JsonObject> ReadJsonResponseAsync();

	private:
		wil::unique_hfile m_pipe;
		std::wstring m_pipePath;
	};

	template<IpcJsonRequest TRequest, IpcJsonResponse TResponse>
	inline wil::task<IpcResult<TResponse>> NamedPipeClient::SendAsync(TRequest request)
	{
		co_await winrt::resume_background();
		JsonObject requestJson = request.ToJson();

		JsonObject responseJson =
			co_await SendJsonRequestAsync(
			TRequest::MessageType,
			requestJson);

		co_return IpcResult<TResponse>::FromJson(responseJson);
	}
}

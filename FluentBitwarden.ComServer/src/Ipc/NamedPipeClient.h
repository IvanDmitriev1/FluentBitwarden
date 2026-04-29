#pragma once
#include <pch.h>
#include "IpcProtocol.h"

namespace FluentBitwarden::ComServer::Ipc
{
	class NamedPipeClient final
	{
		std::chrono::milliseconds ConnectTimeout{ std::chrono::seconds{ 5 } };

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
		wil::task<JsonObject> SendJsonRequestAsync(uint16_t messageType, JsonObject json);

		wil::task<void> WritePayload(uint16_t messageType, std::string utf8);
		wil::task<JsonObject> ReadJsonResponseAsync();

	private:
		wil::unique_hfile m_pipe;
		std::wstring m_pipePath;
	};

	template<IpcJsonRequest TRequest, IpcJsonResponse TResponse>
	inline wil::task<TResponse> NamedPipeClient::SendAsync(TRequest request)
	{
		co_await winrt::resume_background();
		JsonObject requestJson = request.ToJson();

		JsonObject responseJson =
			co_await SendJsonRequestAsync(
			TRequest::MessageType,
			requestJson);

		co_return TResponse::FromJson(responseJson);
	}
}

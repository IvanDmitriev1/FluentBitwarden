#pragma once
#include <pch.h>
#include "IpcProtocol.h"

namespace FluentBitwarden::ComServer::Ipc
{
	class AppNamedPipeClient final
	{
		std::chrono::milliseconds ConnectTimeout{ std::chrono::seconds{ 5 } };

	public:
		AppNamedPipeClient();
		~AppNamedPipeClient() = default;

		AppNamedPipeClient(AppNamedPipeClient const&) = delete;
		AppNamedPipeClient& operator=(AppNamedPipeClient const&) = delete;

		AppNamedPipeClient(AppNamedPipeClient&&) noexcept = default;
		AppNamedPipeClient& operator=(AppNamedPipeClient&&) noexcept = default;

	public:
		template <IpcBinaryRequest TRequest, IpcBinaryResponse TResponse>
		[[nodiscard]] wil::task<TResponse> SendAsync(TRequest request);

	private:
		wil::task<std::vector<std::byte>> SendBinaryRequestAsync(uint16_t messageType, std::vector<std::byte> payload);

		wil::task<void> WritePayload(uint16_t messageType, std::vector<std::byte> payload);
		wil::task<std::vector<std::byte>> ReadResponsePayloadAsync();

	private:
		wil::unique_hfile m_pipe;
	};

	template<IpcBinaryRequest TRequest, IpcBinaryResponse TResponse>
	inline wil::task<TResponse> AppNamedPipeClient::SendAsync(TRequest request)
	{
		co_await winrt::resume_background();
		std::vector<std::byte> requestPayload = request.ToPayload();

		std::vector<std::byte> responsePayload =
			co_await SendBinaryRequestAsync(
			TRequest::MessageType,
			std::move(requestPayload));

		co_return TResponse::FromPayload(
			std::span<const std::byte>{ responsePayload.data(), responsePayload.size() });
	}
}

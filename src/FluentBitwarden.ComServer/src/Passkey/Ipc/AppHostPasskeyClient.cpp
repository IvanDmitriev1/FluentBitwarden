#include "pch.h"
#include "Passkey/Ipc/AppHostPasskeyClient.h"
#include "Infrastructure/Ipc/AppNamedPipeClient.h"

#include <utility>

namespace FluentBitwarden::ComServer
{
	AppHostPasskeyClient::AppHostPasskeyClient(std::wstring pipePath)
		: m_pipePath(std::move(pipePath))
	{
	}

	wil::task<WebAuthn::PasskeyMakeCredentialResponse> AppHostPasskeyClient::MakeCredential(
		WebAuthn::PasskeyMakeCredentialRequest request)
	{
		Ipc::AppNamedPipeClient client{ m_pipePath };

		co_return co_await client.SendAsync<
			WebAuthn::PasskeyMakeCredentialRequest,
			WebAuthn::PasskeyMakeCredentialResponse>(std::move(request));
	}

	wil::task<WebAuthn::PasskeyAssertionResponse> AppHostPasskeyClient::GetAssertion(
		WebAuthn::PasskeyGetAssertionRequest request)
	{
		Ipc::AppNamedPipeClient client{ m_pipePath };

		co_return co_await client.SendAsync<
			WebAuthn::PasskeyGetAssertionRequest,
			WebAuthn::PasskeyAssertionResponse>(std::move(request));
	}
}

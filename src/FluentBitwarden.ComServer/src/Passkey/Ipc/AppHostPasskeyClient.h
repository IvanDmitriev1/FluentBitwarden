#pragma once
#include "Passkey/Ipc/IPasskeyClient.h"

#include <string>

namespace FluentBitwarden::ComServer
{
	class AppHostPasskeyClient final : public IPasskeyClient
	{
	public:
		explicit AppHostPasskeyClient(std::wstring pipePath);

		wil::task<WebAuthn::PasskeyMakeCredentialResponse> MakeCredential(
			WebAuthn::PasskeyMakeCredentialRequest request) override;

		wil::task<WebAuthn::PasskeyAssertionResponse> GetAssertion(
			WebAuthn::PasskeyGetAssertionRequest request) override;

	private:
		std::wstring m_pipePath;
	};
}

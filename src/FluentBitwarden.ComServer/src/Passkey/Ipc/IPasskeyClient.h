#pragma once
#include "pch.h"
#include "Passkey/Models/PasskeyAssertionMessage.h"
#include "Passkey/Models/PasskeyMakeCredentialMessage.h"

namespace FluentBitwarden::ComServer
{
	struct IPasskeyClient
	{
		virtual ~IPasskeyClient() = default;

		virtual wil::task<WebAuthn::PasskeyMakeCredentialResponse> MakeCredential(
			WebAuthn::PasskeyMakeCredentialRequest request) = 0;

		virtual wil::task<WebAuthn::PasskeyAssertionResponse> GetAssertion(
			WebAuthn::PasskeyGetAssertionRequest request) = 0;
	};
}

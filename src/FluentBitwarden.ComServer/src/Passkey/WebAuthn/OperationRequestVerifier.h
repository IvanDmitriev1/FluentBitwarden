#pragma once
#include "pch.h"
#include "Passkey/WebAuthn/IOperationRequestVerifier.h"

namespace FluentBitwarden::ComServer::WebAuthn
{
	class OperationRequestVerifier final : public IOperationRequestVerifier
	{
	public:
		void Verify(const WEBAUTHN_PLUGIN_OPERATION_REQUEST& request) const override;
	};
}

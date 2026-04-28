#pragma once
#include "pch.h"

namespace FluentBitwarden::ComServer::WebAuthn::OperationRequestVerifier
{
	HRESULT VerifyOperationRequest(const WEBAUTHN_PLUGIN_OPERATION_REQUEST& request) noexcept;
	HRESULT VerifyCancelRequest(const WEBAUTHN_PLUGIN_CANCEL_OPERATION_REQUEST& request) noexcept;
}


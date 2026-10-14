#pragma once
#include "pch.h"

namespace FluentBitwarden::ComServer::WebAuthn::OperationRequestVerifier
{
	void VerifyOperationRequest(const WEBAUTHN_PLUGIN_OPERATION_REQUEST& request);
}

#pragma once
#include <pch.h>
#include "Messages/PasskeyAssertionMessage.h"

namespace FluentBitwarden::ComServer::WebAuthn::AssertionResponseBuilder
{
    HRESULT BuildResponse(const PasskeyAssertionResponse& assertion, PWEBAUTHN_PLUGIN_OPERATION_RESPONSE response) noexcept;

}
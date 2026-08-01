#pragma once
#include <pch.h>
#include "Passkey/Models/PasskeyAssertionMessage.h"

namespace FluentBitwarden::ComServer::WebAuthn::AssertionResponseBuilder
{
    void BuildResponse(const PasskeyAssertionResponse& assertion, PWEBAUTHN_PLUGIN_OPERATION_RESPONSE response);

}

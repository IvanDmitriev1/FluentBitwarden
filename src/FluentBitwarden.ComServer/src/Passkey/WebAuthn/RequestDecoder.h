#pragma once
#include "Passkey/Models/PasskeyAssertionMessage.h"
#include "Passkey/Models/PasskeyMakeCredentialMessage.h"

namespace FluentBitwarden::ComServer::WebAuthn::RequestDecoder
{
    [[nodiscard]] PasskeyGetAssertionRequest DecodeGetAssertion(
        PCWEBAUTHN_PLUGIN_OPERATION_REQUEST request);

    [[nodiscard]] PasskeyMakeCredentialRequest DecodeMakeCredential(
        PCWEBAUTHN_PLUGIN_OPERATION_REQUEST request);
}

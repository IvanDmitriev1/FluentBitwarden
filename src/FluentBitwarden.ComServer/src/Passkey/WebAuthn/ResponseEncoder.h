#pragma once


#include "Passkey/Models/PasskeyAssertionMessage.h"
#include "Passkey/Models/PasskeyMakeCredentialMessage.h"

namespace FluentBitwarden::ComServer::WebAuthn::ResponseEncoder
{
    void EncodeGetAssertion(
        const PasskeyAssertionResponse& assertion,
        PWEBAUTHN_PLUGIN_OPERATION_RESPONSE response);

    void EncodeMakeCredential(
        const PasskeyMakeCredentialResponse& credential,
        PWEBAUTHN_PLUGIN_OPERATION_RESPONSE response);
}

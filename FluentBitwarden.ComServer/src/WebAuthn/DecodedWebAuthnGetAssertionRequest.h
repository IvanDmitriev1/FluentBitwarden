#pragma once
#include <pch.h>
#include "Messages/PasskeyAssertionMessage.h"

namespace FluentBitwarden::ComServer::WebAuthn
{
    using unique_decoded_request =
        wil::unique_any<
        PWEBAUTHN_CTAPCBOR_GET_ASSERTION_REQUEST,
        decltype(&WebAuthNFreeDecodedGetAssertionRequest),
        WebAuthNFreeDecodedGetAssertionRequest>;

    class DecodedGetAssertionRequest final
    {
    public:
        DecodedGetAssertionRequest() = default;
        ~DecodedGetAssertionRequest() = default;

        DecodedGetAssertionRequest(const DecodedGetAssertionRequest&) = delete;
        DecodedGetAssertionRequest& operator=(const DecodedGetAssertionRequest&) = delete;

        DecodedGetAssertionRequest(DecodedGetAssertionRequest&&) noexcept = default;
        DecodedGetAssertionRequest& operator=(DecodedGetAssertionRequest&&) noexcept = default;

    public:
        static HRESULT Decode(PCWEBAUTHN_PLUGIN_OPERATION_REQUEST request, DecodedGetAssertionRequest& result) noexcept;

        HRESULT ToIpcRequest(PasskeyGetAssertionRequest& result) const noexcept;

    private:
        explicit DecodedGetAssertionRequest(PWEBAUTHN_CTAPCBOR_GET_ASSERTION_REQUEST request) noexcept;


    private:
        unique_decoded_request m_request;
    };
}
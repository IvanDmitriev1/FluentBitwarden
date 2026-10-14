#include "pch.h"
#include "Passkey/WebAuthn/RequestDecoder.h"
#include "Infrastructure/Cryptography/HashingFunctions.h"

namespace FluentBitwarden::ComServer::WebAuthn::RequestDecoder
{
    using unique_get_assertion_request =
        wil::unique_any<
        PWEBAUTHN_CTAPCBOR_GET_ASSERTION_REQUEST,
        decltype(&WebAuthNFreeDecodedGetAssertionRequest),
        WebAuthNFreeDecodedGetAssertionRequest>;

    using unique_make_credential_request =
        wil::unique_any<
        PWEBAUTHN_CTAPCBOR_MAKE_CREDENTIAL_REQUEST,
        decltype(&WebAuthNFreeDecodedMakeCredentialRequest),
        WebAuthNFreeDecodedMakeCredentialRequest>;


    PasskeyGetAssertionRequest DecodeGetAssertion(PCWEBAUTHN_PLUGIN_OPERATION_REQUEST operationRequest)
    {
        PWEBAUTHN_CTAPCBOR_GET_ASSERTION_REQUEST rawRequest = nullptr;

        THROW_IF_FAILED(
            WebAuthNDecodeGetAssertionRequest(
                operationRequest->cbEncodedRequest,
                operationRequest->pbEncodedRequest,
                &rawRequest));

        unique_get_assertion_request decodedRequest{ rawRequest };

        const auto rpIdBytes = std::span<const std::uint8_t>
        {
            reinterpret_cast<const std::uint8_t*>(rawRequest->pbRpId),
            rawRequest->cbRpId
        };

        auto rpIdHash = Utils::ComputeSha256(rpIdBytes);

        THROW_HR_IF(E_INVALIDARG, rawRequest->cbClientDataHash != 32);
        THROW_HR_IF_NULL(E_INVALIDARG, rawRequest->pbClientDataHash);

        std::vector<std::uint8_t> clientDataHash
        {
            rawRequest->pbClientDataHash,
            rawRequest->pbClientDataHash + rawRequest->cbClientDataHash
        };

        return PasskeyGetAssertionRequest
        {
            .RpId = winrt::to_string(rawRequest->pwszRpId),
            .RpIdHash = std::move(rpIdHash),
            .ClientDataHash = std::move(clientDataHash)
        };
    }

    PasskeyMakeCredentialRequest DecodeMakeCredential(PCWEBAUTHN_PLUGIN_OPERATION_REQUEST request)
    {
        PWEBAUTHN_CTAPCBOR_MAKE_CREDENTIAL_REQUEST rawRequest = nullptr;

        THROW_IF_FAILED(
            WebAuthNDecodeMakeCredentialRequest(
                request->cbEncodedRequest,
                request->pbEncodedRequest,
                &rawRequest));

        unique_make_credential_request decodedRequest{ rawRequest };

        THROW_HR_IF(E_INVALIDARG,
            rawRequest->cbRpId == 0 ||
            rawRequest->pbRpId == nullptr ||
            rawRequest->pRpInformation == nullptr ||
            rawRequest->pRpInformation->pwszName == nullptr);

        const auto rpIdBytes = std::span<const std::uint8_t>(rawRequest->pbRpId, rawRequest->cbRpId);
        auto rpIdHash = Utils::ComputeSha256(rpIdBytes);

        std::string rpId(reinterpret_cast<const char*>(rawRequest->pbRpId), rawRequest->cbRpId);
        std::string rpName = winrt::to_string(winrt::hstring{ rawRequest->pRpInformation->pwszName });

        THROW_HR_IF(
            E_INVALIDARG,
            rawRequest->cbClientDataHash != 32 ||
            rawRequest->pbClientDataHash == nullptr);

        std::vector<std::uint8_t> clientDataHash(rawRequest->pbClientDataHash, rawRequest->pbClientDataHash + rawRequest->cbClientDataHash);


        THROW_HR_IF(
            E_INVALIDARG,
            rawRequest->pUserInformation == nullptr ||
            rawRequest->pUserInformation->pbId == nullptr ||
            rawRequest->pUserInformation->cbId == 0 ||
            rawRequest->pUserInformation->pwszName == nullptr);

        std::vector<std::uint8_t> userId(rawRequest->pUserInformation->pbId, rawRequest->pUserInformation->pbId + rawRequest->pUserInformation->cbId);
        std::string userName = winrt::to_string(winrt::hstring{ rawRequest->pUserInformation->pwszName });
        std::string userDisplayName = winrt::to_string(winrt::hstring{ rawRequest->pUserInformation->pwszDisplayName });

        bool requireResidentKey = false;
        bool userVerification = false;

        if (rawRequest->pAuthenticatorOptions != nullptr)
        {
            requireResidentKey = rawRequest->pAuthenticatorOptions->lRequireResidentKey;
            userVerification =rawRequest->pAuthenticatorOptions->lUv;
        }

        return PasskeyMakeCredentialRequest
        {
            .RpId = std::move(rpId),
            .RpName = std::move(rpName),
            .RpIdHash = std::move(rpIdHash),
            .ClientDataHash = std::move(clientDataHash),
            .UserId = std::move(userId),
            .UserName = std::move(userName),
            .UserDisplayName = std::move(userDisplayName),
            .RequireResidentKey = requireResidentKey,
            .UserVerification = userVerification
        };
    }
}

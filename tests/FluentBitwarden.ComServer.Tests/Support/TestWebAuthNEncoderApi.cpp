#include "pch.h"

#include <Windows.h>
#include <webauthn.h>
#include <webauthnplugin.h>

#include <algorithm>
#include <array>
#include <string_view>

namespace
{
    constexpr std::array<BYTE, 3> MakeCredentialEncoded{ 0x4d, 0x43, 0x01 };
    constexpr std::array<BYTE, 3> GetAssertionEncoded{ 0x47, 0x41, 0x01 };
    constexpr std::array<BYTE, 2> ExpectedMakeCredentialAuthenticatorData{ 0x11, 0x22 };
    constexpr std::array<BYTE, 3> ExpectedAssertionAuthenticatorData{ 0x31, 0x32, 0x33 };
    constexpr std::array<BYTE, 2> ExpectedAssertionCredentialId{ 0xa1, 0xa2 };
    constexpr std::array<BYTE, 1> ExpectedAssertionUserId{ 0xb1 };
    constexpr std::array<BYTE, 2> ExpectedAssertionSignature{ 0xd1, 0xd2 };

    template<std::size_t Size>
    bool Matches(DWORD length, const BYTE* bytes, const std::array<BYTE, Size>& expected)
    {
        return length == expected.size() && bytes != nullptr &&
            std::equal(expected.begin(), expected.end(), bytes);
    }

    template<std::size_t Size>
    bool ReturnMalformedOutput(
        DWORD inputSize, const BYTE* input, const std::array<BYTE, Size>& encoded,
        DWORD* encodedSize, BYTE** encodedResponse)
    {
        if (inputSize != 1 || input == nullptr || input[0] < 0xe0 || input[0] > 0xe2)
        {
            return false;
        }

        // Test tags: zero size, null buffer, or both invalid despite S_OK.
        *encodedSize = input[0] == 0xe1 ? static_cast<DWORD>(encoded.size()) : 0;
        *encodedResponse = input[0] == 0xe0 ? const_cast<BYTE*>(encoded.data()) : nullptr;
        return true;
    }
}

HRESULT WINAPI WebAuthNEncodeMakeCredentialResponse(
    PCWEBAUTHN_CREDENTIAL_ATTESTATION attestation,
    DWORD* encodedSize,
    BYTE** encodedResponse)
{
    if (attestation == nullptr || encodedSize == nullptr || encodedResponse == nullptr)
    {
        return E_POINTER;
    }

    if (ReturnMalformedOutput(attestation->cbAuthenticatorData, attestation->pbAuthenticatorData,
        MakeCredentialEncoded, encodedSize, encodedResponse))
    {
        return S_OK;
    }

    if (attestation->cbAuthenticatorData == 1 && attestation->pbAuthenticatorData != nullptr &&
        attestation->pbAuthenticatorData[0] == 0xee)
    {
        return E_ACCESSDENIED;
    }

    if (attestation->dwVersion != WEBAUTHN_CREDENTIAL_ATTESTATION_CURRENT_VERSION ||
        !Matches(attestation->cbAuthenticatorData, attestation->pbAuthenticatorData,
            ExpectedMakeCredentialAuthenticatorData) ||
        attestation->pwszFormatType == nullptr ||
        std::wstring_view{ attestation->pwszFormatType } != WEBAUTHN_ATTESTATION_TYPE_NONE ||
        attestation->cbAttestation != 0 || attestation->pbAttestation != nullptr)
    {
        return E_UNEXPECTED;
    }

    *encodedSize = static_cast<DWORD>(MakeCredentialEncoded.size());
    *encodedResponse = const_cast<BYTE*>(MakeCredentialEncoded.data());
    return S_OK;
}

HRESULT WINAPI WebAuthNEncodeGetAssertionResponse(
    PCWEBAUTHN_CTAPCBOR_GET_ASSERTION_RESPONSE assertionResponse,
    DWORD* encodedSize,
    BYTE** encodedResponse)
{
    if (assertionResponse == nullptr || encodedSize == nullptr || encodedResponse == nullptr)
    {
        return E_POINTER;
    }

    const WEBAUTHN_ASSERTION& assertion = assertionResponse->WebAuthNAssertion;
    if (ReturnMalformedOutput(assertion.cbSignature, assertion.pbSignature,
        GetAssertionEncoded, encodedSize, encodedResponse))
    {
        return S_OK;
    }
    if (assertion.cbSignature == 1 && assertion.pbSignature != nullptr &&
        assertion.pbSignature[0] == 0xee)
    {
        return E_ACCESSDENIED;
    }

    if (assertion.dwVersion != WEBAUTHN_ASSERTION_CURRENT_VERSION ||
        assertion.Credential.dwVersion != WEBAUTHN_CREDENTIAL_CURRENT_VERSION ||
        !Matches(assertion.Credential.cbId, assertion.Credential.pbId,
            ExpectedAssertionCredentialId) ||
        assertion.Credential.pwszCredentialType == nullptr ||
        std::wstring_view{ assertion.Credential.pwszCredentialType } != WEBAUTHN_CREDENTIAL_TYPE_PUBLIC_KEY ||
        !Matches(assertion.cbUserId, assertion.pbUserId, ExpectedAssertionUserId) ||
        !Matches(assertion.cbAuthenticatorData, assertion.pbAuthenticatorData,
            ExpectedAssertionAuthenticatorData) ||
        !Matches(assertion.cbSignature, assertion.pbSignature, ExpectedAssertionSignature) ||
        assertionResponse->pUserInformation == nullptr ||
        assertionResponse->pUserInformation->dwVersion != WEBAUTHN_USER_ENTITY_INFORMATION_CURRENT_VERSION ||
        !Matches(assertionResponse->pUserInformation->cbId,
            assertionResponse->pUserInformation->pbId, ExpectedAssertionUserId) ||
        assertionResponse->pUserInformation->pwszName == nullptr ||
        std::wstring_view{ assertionResponse->pUserInformation->pwszName } != L"alice" ||
        assertionResponse->pUserInformation->pwszDisplayName == nullptr ||
        std::wstring_view{ assertionResponse->pUserInformation->pwszDisplayName } != L"Alice" ||
        assertionResponse->pUserInformation->pwszIcon != nullptr ||
        assertionResponse->dwNumberOfCredentials != 0 ||
        assertionResponse->lUserSelected != 0 ||
        assertionResponse->cbLargeBlobKey != 0 || assertionResponse->pbLargeBlobKey != nullptr ||
        assertionResponse->cbUnsignedExtensionOutputs != 0 ||
        assertionResponse->pbUnsignedExtensionOutputs != nullptr)
    {
        return E_UNEXPECTED;
    }

    *encodedSize = static_cast<DWORD>(GetAssertionEncoded.size());
    *encodedResponse = const_cast<BYTE*>(GetAssertionEncoded.data());
    return S_OK;
}

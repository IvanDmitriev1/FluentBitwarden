#include "pch.h"
#include "Modules/Passkey/WebAuthn/OperationRequestVerifier.h"
#include "Modules/Passkey/Registration/PluginRegistrationManager.h"
#include "Infrastructure/Cryptography/HashingFunctions.h"

#include <ncrypt.h>

using unique_ncrypt_prov = wil::unique_any<
    NCRYPT_PROV_HANDLE,
    decltype(&NCryptFreeObject),
    NCryptFreeObject>;

using unique_ncrypt_key = wil::unique_any<
    NCRYPT_KEY_HANDLE,
    decltype(&NCryptFreeObject),
    NCryptFreeObject>;

namespace FluentBitwarden::ComServer::WebAuthn::OperationRequestVerifier
{
    void VerifyOperationRequest(
        const WEBAUTHN_PLUGIN_OPERATION_REQUEST& request)
    {
        THROW_HR_IF(
            E_INVALIDARG,
            request.requestType != WEBAUTHN_PLUGIN_REQUEST_TYPE_CTAP2_CBOR);

        THROW_HR_IF(
            E_INVALIDARG,
            request.cbEncodedRequest == 0 || request.pbEncodedRequest == nullptr);

        THROW_HR_IF(
            E_INVALIDARG,
            request.cbRequestSignature == 0 || request.pbRequestSignature == nullptr);

        std::vector<std::uint8_t> publicKeyBlob =
            PluginRegistrationManager::GetOperationSigningPublicKey();

        THROW_HR_IF(E_INVALIDARG, publicKeyBlob.size() < sizeof(BCRYPT_KEY_BLOB));

        const auto signedBuffer = std::span<const std::uint8_t>(
            reinterpret_cast<const std::uint8_t*>(request.pbEncodedRequest),
            request.cbEncodedRequest);

        const auto signature = std::span<const std::uint8_t>(
            reinterpret_cast<const std::uint8_t*>(request.pbRequestSignature),
            request.cbRequestSignature);

        unique_ncrypt_prov provider;
        THROW_IF_FAILED(NCryptOpenStorageProvider(
            &provider,
            nullptr,
            0));

        unique_ncrypt_key publicKey;
        THROW_IF_FAILED(NCryptImportKey(
            provider.get(),
            0,
            BCRYPT_PUBLIC_KEY_BLOB,
            nullptr,
            &publicKey,
            const_cast<PBYTE>(publicKeyBlob.data()),
            static_cast<DWORD>(publicKeyBlob.size()),
            0));

        std::vector<std::uint8_t> hash = Utils::ComputeSha256(signedBuffer);

        THROW_HR_IF(E_INVALIDARG, hash.size() != 32);

        const auto* keyBlob =
            reinterpret_cast<const BCRYPT_KEY_BLOB*>(publicKeyBlob.data());

        const bool isRsa =
            keyBlob->Magic == BCRYPT_RSAPUBLIC_MAGIC;

        const bool isEcdsa =
            keyBlob->Magic == BCRYPT_ECDSA_PUBLIC_P256_MAGIC ||
            keyBlob->Magic == BCRYPT_ECDSA_PUBLIC_P384_MAGIC ||
            keyBlob->Magic == BCRYPT_ECDSA_PUBLIC_P521_MAGIC;

        THROW_HR_IF(E_INVALIDARG, !isRsa && !isEcdsa);

        void* paddingInfo = nullptr;
        DWORD flags = 0;

        BCRYPT_PKCS1_PADDING_INFO rsaPaddingInfo{};

        if (isRsa)
        {
            rsaPaddingInfo.pszAlgId = BCRYPT_SHA256_ALGORITHM;
            paddingInfo = &rsaPaddingInfo;
            flags = BCRYPT_PAD_PKCS1;
        }

        THROW_IF_FAILED(NCryptVerifySignature(
            publicKey.get(),
            paddingInfo,
            hash.data(),
            static_cast<DWORD>(hash.size()),
            const_cast<PBYTE>(signature.data()),
            static_cast<DWORD>(signature.size()),
            flags));
    }

}

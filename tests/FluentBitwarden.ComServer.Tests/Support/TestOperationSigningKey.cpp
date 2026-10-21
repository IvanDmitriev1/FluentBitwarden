#include "pch.h"

#include "TestOperationSigningKey.h"
#include "Passkey/Registration/PluginRegistrationManager.h"
#include "Infrastructure/Cryptography/HashingFunctions.h"

#include <bcrypt.h>
#include <mutex>
#include <memory>

namespace FluentBitwarden::ComServer::Tests::Support
{
    namespace
    {
        using UniqueBcryptHandle = std::unique_ptr<void, void(*)(void*)>;

        void CloseAlgorithm(void* handle) noexcept
        {
            (void)BCryptCloseAlgorithmProvider(static_cast<BCRYPT_ALG_HANDLE>(handle), 0);
        }

        void DestroyKey(void* handle) noexcept
        {
            (void)BCryptDestroyKey(static_cast<BCRYPT_KEY_HANDLE>(handle));
        }

        class SigningKey final
        {
        public:
            SigningKey()
            {
                BCRYPT_ALG_HANDLE algorithm{};
                THROW_IF_NTSTATUS_FAILED(BCryptOpenAlgorithmProvider(
                    &algorithm,
                    BCRYPT_RSA_ALGORITHM,
                    nullptr,
                    0));
                UniqueBcryptHandle algorithmOwner{ algorithm, CloseAlgorithm };

                BCRYPT_KEY_HANDLE key{};
                THROW_IF_NTSTATUS_FAILED(BCryptGenerateKeyPair(
                    algorithmOwner.get(),
                    &key,
                    2048,
                    0));
                UniqueBcryptHandle keyOwner{ key, DestroyKey };
                THROW_IF_NTSTATUS_FAILED(BCryptFinalizeKeyPair(keyOwner.get(), 0));

                ULONG publicKeySize{};
                THROW_IF_NTSTATUS_FAILED(BCryptExportKey(
                    keyOwner.get(),
                    nullptr,
                    BCRYPT_RSAPUBLIC_BLOB,
                    nullptr,
                    0,
                    &publicKeySize,
                    0));
                m_publicKey.resize(publicKeySize);
                THROW_IF_NTSTATUS_FAILED(BCryptExportKey(
                    keyOwner.get(),
                    nullptr,
                    BCRYPT_RSAPUBLIC_BLOB,
                    m_publicKey.data(),
                    publicKeySize,
                    &publicKeySize,
                    0));

                ULONG signatureSize{};
                ULONG resultSize{};
                THROW_IF_NTSTATUS_FAILED(BCryptGetProperty(
                    keyOwner.get(),
                    BCRYPT_SIGNATURE_LENGTH,
                    reinterpret_cast<PUCHAR>(&signatureSize),
                    sizeof(signatureSize),
                    &resultSize,
                    0));
                m_signatureSize = signatureSize;

                m_algorithm = algorithmOwner.release();
                m_key = keyOwner.release();
            }

            ~SigningKey()
            {
                if (m_key != nullptr)
                {
                    (void)BCryptDestroyKey(m_key);
                }
                if (m_algorithm != nullptr)
                {
                    (void)BCryptCloseAlgorithmProvider(m_algorithm, 0);
                }
            }

            [[nodiscard]] const std::vector<std::uint8_t>& PublicKeyBlob() const noexcept
            {
                return m_publicKey;
            }

            [[nodiscard]] std::vector<std::uint8_t> Sign(
                std::span<const std::uint8_t> request) const
            {
                const std::vector<std::uint8_t> hash =
                    FluentBitwarden::ComServer::Utils::ComputeSha256(request);
                BCRYPT_PKCS1_PADDING_INFO paddingInfo{};
                paddingInfo.pszAlgId = BCRYPT_SHA256_ALGORITHM;

                std::vector<std::uint8_t> signature(m_signatureSize);
                ULONG bytesWritten{};
                THROW_IF_NTSTATUS_FAILED(BCryptSignHash(
                    m_key,
                    &paddingInfo,
                    const_cast<PUCHAR>(hash.data()),
                    static_cast<ULONG>(hash.size()),
                    signature.data(),
                    static_cast<ULONG>(signature.size()),
                    &bytesWritten,
                    BCRYPT_PAD_PKCS1));
                signature.resize(bytesWritten);
                return signature;
            }

            SigningKey(const SigningKey&) = delete;
            SigningKey& operator=(const SigningKey&) = delete;

        private:
            BCRYPT_ALG_HANDLE m_algorithm{};
            BCRYPT_KEY_HANDLE m_key{};
            std::vector<std::uint8_t> m_publicKey;
            ULONG m_signatureSize{};
        };

        SigningKey& GetSigningKey()
        {
            static SigningKey key;
            return key;
        }

        std::mutex& GetSigningMutex()
        {
            static std::mutex mutex;
            return mutex;
        }
    }

    const std::vector<std::uint8_t>& TestOperationSigningKey::PublicKeyBlob()
    {
        return GetSigningKey().PublicKeyBlob();
    }

    std::vector<std::uint8_t> TestOperationSigningKey::Sign(
        std::span<const std::uint8_t> request)
    {
        const std::lock_guard lock{ GetSigningMutex() };
        return GetSigningKey().Sign(request);
    }
}

namespace FluentBitwarden::ComServer::PluginRegistrationManager
{
    std::vector<std::uint8_t> GetOperationSigningPublicKey()
    {
        return Tests::Support::TestOperationSigningKey::PublicKeyBlob();
    }
}

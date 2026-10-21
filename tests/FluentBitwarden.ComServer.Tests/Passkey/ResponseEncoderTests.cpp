#include "pch.h"
#include "Passkey/WebAuthn/ResponseEncoder.h"

#include <array>
#include <string>
#include <vector>

using namespace Microsoft::VisualStudio::CppUnitTestFramework;
using namespace FluentBitwarden::ComServer::WebAuthn;

namespace FluentBitwarden::ComServer::Tests
{
    namespace
    {
        template <typename Action>
        HRESULT CaptureResult(Action&& action)
        {
            try
            {
                action();
                return S_OK;
            }
            catch (...)
            {
                return wil::ResultFromCaughtException();
            }
        }

        void SetSentinelResponse(
            WEBAUTHN_PLUGIN_OPERATION_RESPONSE& response,
            std::array<BYTE, 1>& encodedResponse)
        {
            response.cbEncodedResponse = static_cast<DWORD>(encodedResponse.size());
            response.pbEncodedResponse = encodedResponse.data();
        }

        void AssertSentinelResponse(
            const WEBAUTHN_PLUGIN_OPERATION_RESPONSE& response,
            const std::array<BYTE, 1>& encodedResponse)
        {
            Assert::AreEqual(static_cast<DWORD>(encodedResponse.size()), response.cbEncodedResponse);
            Assert::IsTrue(response.pbEncodedResponse == encodedResponse.data());
        }
    }

    TEST_CLASS(ResponseEncoderTests)
    {
    public:
        TEST_METHOD(EncodeMakeCredentialRejectsEmptyAuthenticatorDataBeforeWritingOutput)
        {
            const PasskeyMakeCredentialResponse credential{};
            std::array<BYTE, 1> previousResponse{ 0x31 };
            WEBAUTHN_PLUGIN_OPERATION_RESPONSE response{};
            SetSentinelResponse(response, previousResponse);

            const HRESULT result = CaptureResult([&]
            {
                ResponseEncoder::EncodeMakeCredential(credential, &response);
            });

            Assert::AreEqual(E_INVALIDARG, result);
            AssertSentinelResponse(response, previousResponse);
        }

        TEST_METHOD(EncodeGetAssertionRejectsEachMissingRequiredFieldBeforeWritingOutput)
        {
            enum class MissingField
            {
                CredentialId,
                UserId,
                AuthenticatorData,
                Signature,
                UserName,
                UserDisplayName
            };

            constexpr std::array fields{
                MissingField::CredentialId,
                MissingField::UserId,
                MissingField::AuthenticatorData,
                MissingField::Signature,
                MissingField::UserName,
                MissingField::UserDisplayName
            };

            for (const auto field : fields)
            {
                PasskeyAssertionResponse assertion{
                    .CredentialId = { 0x01 },
                    .UserId = { 0x02 },
                    .AuthenticatorData = { 0x03 },
                    .Signature = { 0x04 },
                    .UserName = "alice",
                    .UserDisplayName = "Alice"
                };
                switch (field)
                {
                case MissingField::CredentialId:
                    assertion.CredentialId.clear();
                    break;
                case MissingField::UserId:
                    assertion.UserId.clear();
                    break;
                case MissingField::AuthenticatorData:
                    assertion.AuthenticatorData.clear();
                    break;
                case MissingField::Signature:
                    assertion.Signature.clear();
                    break;
                case MissingField::UserName:
                    assertion.UserName.clear();
                    break;
                case MissingField::UserDisplayName:
                    assertion.UserDisplayName.clear();
                    break;
                }

                std::array<BYTE, 1> previousResponse{ 0x51 };
                WEBAUTHN_PLUGIN_OPERATION_RESPONSE response{};
                SetSentinelResponse(response, previousResponse);
                const HRESULT result = CaptureResult([&]
                {
                    ResponseEncoder::EncodeGetAssertion(assertion, &response);
                });

                Assert::AreEqual(E_INVALIDARG, result);
                AssertSentinelResponse(response, previousResponse);
            }
        }
    };
}

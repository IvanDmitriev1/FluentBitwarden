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
        TEST_METHOD(EncodeMakeCredentialMapsOutputAndPreservesItOnApiFailure)
        {
            const PasskeyMakeCredentialResponse credential{ .AuthenticatorData = { 0x11, 0x22 } };
            WEBAUTHN_PLUGIN_OPERATION_RESPONSE response{};

            ResponseEncoder::EncodeMakeCredential(credential, &response);

            Assert::AreEqual(DWORD{ 3 }, response.cbEncodedResponse);
            Assert::IsNotNull(response.pbEncodedResponse);
            Assert::IsTrue(std::vector<BYTE>(response.pbEncodedResponse,
                response.pbEncodedResponse + response.cbEncodedResponse) ==
                std::vector<BYTE>{ 0x4d, 0x43, 0x01 });

            std::array<BYTE, 1> previousResponse{ 0x31 };
            SetSentinelResponse(response, previousResponse);
            const PasskeyMakeCredentialResponse rejected{ .AuthenticatorData = { 0xee } };
            Assert::AreEqual(E_ACCESSDENIED, CaptureResult([&]
            {
                ResponseEncoder::EncodeMakeCredential(rejected, &response);
            }));
            AssertSentinelResponse(response, previousResponse);
        }

        TEST_METHOD(EncodeGetAssertionMapsOutputAndPreservesItOnApiFailure)
        {
            auto assertion = ValidAssertion();
            WEBAUTHN_PLUGIN_OPERATION_RESPONSE response{};

            ResponseEncoder::EncodeGetAssertion(assertion, &response);

            Assert::AreEqual(DWORD{ 3 }, response.cbEncodedResponse);
            Assert::IsNotNull(response.pbEncodedResponse);
            Assert::IsTrue(std::vector<BYTE>(response.pbEncodedResponse,
                response.pbEncodedResponse + response.cbEncodedResponse) ==
                std::vector<BYTE>{ 0x47, 0x41, 0x01 });

            std::array<BYTE, 1> previousResponse{ 0x31 };
            SetSentinelResponse(response, previousResponse);
            assertion.Signature = { 0xee };
            Assert::AreEqual(E_ACCESSDENIED, CaptureResult([&]
            {
                ResponseEncoder::EncodeGetAssertion(assertion, &response);
            }));
            AssertSentinelResponse(response, previousResponse);
        }

        TEST_METHOD(BothEncodersRejectZeroSizeOrNullApiBuffersBeforeWritingOutput)
        {
            // The test-only Windows API substitutes use these tags for zero-size,
            // null-buffer, and both-invalid successful API results, respectively.
            for (const BYTE tag : { BYTE{ 0xe0 }, BYTE{ 0xe1 }, BYTE{ 0xe2 } })
            {
                const PasskeyMakeCredentialResponse credential{ .AuthenticatorData = { tag } };
                auto assertion = ValidAssertion();
                assertion.Signature = { tag };
                std::array<BYTE, 1> previousResponse{ 0x31 };
                WEBAUTHN_PLUGIN_OPERATION_RESPONSE response{};

                SetSentinelResponse(response, previousResponse);
                Assert::AreEqual(E_UNEXPECTED, CaptureResult([&]
                {
                    ResponseEncoder::EncodeMakeCredential(credential, &response);
                }), L"MakeCredential must reject a malformed successful API result");
                AssertSentinelResponse(response, previousResponse);

                SetSentinelResponse(response, previousResponse);
                Assert::AreEqual(E_UNEXPECTED, CaptureResult([&]
                {
                    ResponseEncoder::EncodeGetAssertion(assertion, &response);
                }), L"GetAssertion must reject a malformed successful API result");
                AssertSentinelResponse(response, previousResponse);
            }
        }

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
                auto assertion = ValidAssertion();
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
    private:
        static PasskeyAssertionResponse ValidAssertion()
        {
            return {
                .CredentialId = { 0xa1, 0xa2 },
                .UserId = { 0xb1 },
                .AuthenticatorData = { 0x31, 0x32, 0x33 },
                .Signature = { 0xd1, 0xd2 },
                .UserName = "alice",
                .UserDisplayName = "Alice"
            };
        }
    };
}

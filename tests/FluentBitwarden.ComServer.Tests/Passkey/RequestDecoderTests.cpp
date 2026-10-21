#include "pch.h"
#include "Passkey/WebAuthn/RequestDecoder.h"

#include <array>
#include <cstdint>
#include <vector>

using namespace Microsoft::VisualStudio::CppUnitTestFramework;
using namespace FluentBitwarden::ComServer::WebAuthn;

namespace FluentBitwarden::ComServer::Tests
{
    namespace
    {
        constexpr std::array<std::uint8_t, 32> ClientDataHash{
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
            0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f,
            0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
            0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e, 0x1f
        };

        constexpr std::array<std::uint8_t, 32> ExampleRpIdHash{
            0xa3, 0x79, 0xa6, 0xf6, 0xee, 0xaf, 0xb9, 0xa5,
            0x5e, 0x37, 0x8c, 0x11, 0x80, 0x34, 0xe2, 0x75,
            0x1e, 0x68, 0x2f, 0xab, 0x9f, 0x2d, 0x30, 0xab,
            0x13, 0xd2, 0x12, 0x55, 0x86, 0xce, 0x19, 0x47
        };

        WEBAUTHN_PLUGIN_OPERATION_REQUEST RequestFor(std::vector<std::uint8_t>& encodedRequest)
        {
            WEBAUTHN_PLUGIN_OPERATION_REQUEST request{};
            request.requestType = WEBAUTHN_PLUGIN_REQUEST_TYPE_CTAP2_CBOR;
            request.cbEncodedRequest = static_cast<DWORD>(encodedRequest.size());
            request.pbEncodedRequest = encodedRequest.data();
            return request;
        }

        template<typename Action>
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

        std::vector<std::uint8_t> MakeCredentialCbor(
            bool includeOptions,
            std::uint8_t requireResidentKey,
            std::uint8_t userVerification)
        {
            std::vector<std::uint8_t> encoded{
                static_cast<std::uint8_t>(includeOptions ? 0xa6 : 0xa5),
                0x01, 0x58, 0x20,
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
                0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f,
                0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
                0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e, 0x1f,
                0x02, 0xa2,
                0x62, 0x69, 0x64, 0x6b, 0x65, 0x78, 0x61, 0x6d, 0x70, 0x6c, 0x65, 0x2e, 0x63, 0x6f, 0x6d,
                0x64, 0x6e, 0x61, 0x6d, 0x65, 0x67, 0x45, 0x78, 0x61, 0x6d, 0x70, 0x6c, 0x65,
                0x03, 0xa3,
                0x62, 0x69, 0x64, 0x41, 0x01,
                0x64, 0x6e, 0x61, 0x6d, 0x65, 0x65, 0x61, 0x6c, 0x69, 0x63, 0x65,
                0x6b, 0x64, 0x69, 0x73, 0x70, 0x6c, 0x61, 0x79, 0x4e, 0x61, 0x6d, 0x65, 0x65, 0x41, 0x6c, 0x69, 0x63, 0x65,
                0x04, 0x81, 0xa2,
                0x64, 0x74, 0x79, 0x70, 0x65, 0x6a, 0x70, 0x75, 0x62, 0x6c, 0x69, 0x63, 0x2d, 0x6b, 0x65, 0x79,
                0x63, 0x61, 0x6c, 0x67, 0x26,
                0x05, 0x81, 0xa2,
                0x62, 0x69, 0x64, 0x42, 0x03, 0x04,
                0x64, 0x74, 0x79, 0x70, 0x65, 0x6a, 0x70, 0x75, 0x62, 0x6c, 0x69, 0x63, 0x2d, 0x6b, 0x65, 0x79
            };

            if (includeOptions)
            {
                encoded.insert(encoded.end(), {
                    0x07, 0xa2,
                    0x62, 0x72, 0x6b, requireResidentKey,
                    0x62, 0x75, 0x76, userVerification
                });
            }

            return encoded;
        }

        std::vector<std::uint8_t> GetAssertionCbor()
        {
            return {
                0xa3,
                0x01, 0x6b, 0x65, 0x78, 0x61, 0x6d, 0x70, 0x6c, 0x65, 0x2e, 0x63, 0x6f, 0x6d,
                0x02, 0x58, 0x20,
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
                0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f,
                0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
                0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e, 0x1f,
                0x03, 0x81, 0xa2,
                0x62, 0x69, 0x64, 0x42, 0x01, 0x02,
                0x64, 0x74, 0x79, 0x70, 0x65, 0x6a, 0x70, 0x75, 0x62, 0x6c, 0x69, 0x63, 0x2d, 0x6b, 0x65, 0x79
            };
        }
    }

    TEST_CLASS(RequestDecoderTests)
    {
    public:
        TEST_METHOD(DecodeGetAssertionMapsRpIdHashAndClientDataHashFromLiteralCbor)
        {
            auto encoded = GetAssertionCbor();
            const auto operationRequest = RequestFor(encoded);

            const PasskeyGetAssertionRequest request = RequestDecoder::DecodeGetAssertion(&operationRequest);

            Assert::AreEqual(std::string{ "example.com" }, request.RpId);
            Assert::IsTrue(request.RpIdHash ==
                std::vector<std::uint8_t>(ExampleRpIdHash.begin(), ExampleRpIdHash.end()));
            Assert::IsTrue(request.ClientDataHash ==
                std::vector<std::uint8_t>(ClientDataHash.begin(), ClientDataHash.end()));
        }

        TEST_METHOD(DecodeGetAssertionRejectsATopLevelCborBreak)
        {
            // CBOR break (0xff) is only valid inside an indefinite-length item.
            std::vector<std::uint8_t> encoded{ 0xff };
            const auto operationRequest = RequestFor(encoded);

            const HRESULT result = CaptureResult([&]
            {
                (void)RequestDecoder::DecodeGetAssertion(&operationRequest);
            });

            Assert::IsTrue(FAILED(result));
        }

        TEST_METHOD(DecodeMakeCredentialMapsRpUserHashesAndOptionsFromLiteralCbor)
        {
            struct OptionCase
            {
                bool includeOptions;
                std::uint8_t residentKey;
                std::uint8_t userVerification;
                bool expectedResidentKey;
                bool expectedUserVerification;
            };

            constexpr std::array cases{
                OptionCase{ true, 0xf5, 0xf5, true, true },
                OptionCase{ true, 0xf4, 0xf5, false, true },
                OptionCase{ false, 0xf5, 0xf5, false, false }
            };

            for (const auto& testCase : cases)
            {
                auto encoded = MakeCredentialCbor(
                    testCase.includeOptions,
                    testCase.residentKey,
                    testCase.userVerification);
                const auto operationRequest = RequestFor(encoded);

                const PasskeyMakeCredentialRequest request = RequestDecoder::DecodeMakeCredential(&operationRequest);

                Assert::AreEqual(std::string{ "example.com" }, request.RpId);
                Assert::AreEqual(std::string{ "Example" }, request.RpName);
                Assert::IsTrue(request.RpIdHash ==
                    std::vector<std::uint8_t>(ExampleRpIdHash.begin(), ExampleRpIdHash.end()));
                Assert::IsTrue(request.ClientDataHash ==
                    std::vector<std::uint8_t>(ClientDataHash.begin(), ClientDataHash.end()));
                Assert::IsTrue(request.UserId == std::vector<std::uint8_t>{ 0x01 });
                Assert::AreEqual(std::string{ "alice" }, request.UserName);
                Assert::AreEqual(std::string{ "Alice" }, request.UserDisplayName);
                Assert::AreEqual(testCase.expectedResidentKey, request.RequireResidentKey);
                Assert::AreEqual(testCase.expectedUserVerification, request.UserVerification);
            }
        }

        TEST_METHOD(DecodeMakeCredentialRejectsATopLevelCborBreak)
        {
            // CTAP2 MakeCredential requests are CBOR maps; a top-level break is malformed CBOR.
            std::vector<std::uint8_t> encoded{ 0xff };
            const auto operationRequest = RequestFor(encoded);

            const HRESULT result = CaptureResult([&]
            {
                (void)RequestDecoder::DecodeMakeCredential(&operationRequest);
            });

            Assert::IsTrue(FAILED(result));
        }
    };
}

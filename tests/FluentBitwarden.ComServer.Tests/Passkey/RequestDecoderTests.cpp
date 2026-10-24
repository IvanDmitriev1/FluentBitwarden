#include "pch.h"
#include "Passkey/WebAuthn/RequestDecoder.h"
#include "Passkey/PluginAuthenticator.h"
#include "../Support/FakeAppHostLauncher.h"
#include "../Support/FakePasskeyClient.h"
#include "../Support/TestOperationSigningKey.h"

#include <array>
#include <cstdint>
#include <ncrypt.h>
#include <string>
#include <vector>

using namespace Microsoft::VisualStudio::CppUnitTestFramework;
using namespace FluentBitwarden::ComServer;
using namespace FluentBitwarden::ComServer::Tests::Support;
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

        struct SignedOperationRequest
        {
            std::vector<std::uint8_t> encodedRequest;
            std::vector<std::uint8_t> signature;
            WEBAUTHN_PLUGIN_OPERATION_REQUEST request{};

            explicit SignedOperationRequest(std::vector<std::uint8_t> bytes)
                : encodedRequest(std::move(bytes)),
                  signature(TestOperationSigningKey::Sign(encodedRequest))
            {
                request.requestType = WEBAUTHN_PLUGIN_REQUEST_TYPE_CTAP2_CBOR;
                request.cbEncodedRequest = static_cast<DWORD>(encodedRequest.size());
                request.pbEncodedRequest = encodedRequest.data();
                request.cbRequestSignature = static_cast<DWORD>(signature.size());
                request.pbRequestSignature = signature.data();
            }
        };

        struct PluginAuthenticatorHarness
        {
            std::vector<std::string> calls;
            FakePasskeyClient* passkeyClient{};
            FakeAppHostLauncher* appHostLauncher{};
            winrt::com_ptr<IPluginAuthenticator> authenticator;

            PluginAuthenticatorHarness()
            {
                auto clientDouble = std::make_unique<FakePasskeyClient>(calls);
                auto launcherDouble = std::make_unique<FakeAppHostLauncher>(calls);
                passkeyClient = clientDouble.get();
                appHostLauncher = launcherDouble.get();
                authenticator = winrt::make<PluginAuthenticator>(
                    std::move(clientDouble),
                    std::move(launcherDouble));
            }
        };

        void SetSentinelResponse(
            WEBAUTHN_PLUGIN_OPERATION_RESPONSE& response,
            std::array<BYTE, 1>& sentinel)
        {
            sentinel[0] = 0x7a;
            response.cbEncodedResponse = static_cast<DWORD>(sentinel.size());
            response.pbEncodedResponse = sentinel.data();
        }

        void AssertResponseIsEmpty(const WEBAUTHN_PLUGIN_OPERATION_RESPONSE& response)
        {
            Assert::AreEqual(static_cast<DWORD>(0), response.cbEncodedResponse);
            Assert::IsTrue(response.pbEncodedResponse == nullptr);
        }

        void ReplaceCborByteString(
            std::vector<std::uint8_t>& encoded,
            std::size_t headerOffset,
            std::size_t previousLength,
            std::size_t newLength)
        {
            std::vector<std::uint8_t> replacement;
            if (newLength < 24)
            {
                replacement.push_back(static_cast<std::uint8_t>(0x40 | newLength));
            }
            else
            {
                replacement.insert(replacement.end(), { 0x58, static_cast<std::uint8_t>(newLength) });
            }

            for (std::size_t index = 0; index < newLength; ++index)
            {
                replacement.push_back(index < ClientDataHash.size()
                    ? ClientDataHash[index]
                    : std::uint8_t{ 0x20 });
            }

            encoded.erase(
                encoded.begin() + static_cast<std::ptrdiff_t>(headerOffset),
                encoded.begin() + static_cast<std::ptrdiff_t>(headerOffset + 2 + previousLength));
            encoded.insert(
                encoded.begin() + static_cast<std::ptrdiff_t>(headerOffset),
                replacement.begin(),
                replacement.end());
        }

        std::vector<std::uint8_t> MakeCredentialCbor(
            bool includeOptions,
            std::uint8_t requireResidentKey,
            std::uint8_t userVerification,
            std::size_t clientDataHashLength = ClientDataHash.size())
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

            ReplaceCborByteString(encoded, 2, ClientDataHash.size(), clientDataHashLength);

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

        std::vector<std::uint8_t> GetAssertionCbor(std::size_t clientDataHashLength = ClientDataHash.size())
        {
            std::vector<std::uint8_t> encoded{
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
            ReplaceCborByteString(encoded, 15, ClientDataHash.size(), clientDataHashLength);
            return encoded;
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

        TEST_METHOD(DecodeGetAssertionRejectsClientDataHashLengthsOutsideThirtyTwoBytes)
        {
            for (const std::size_t length : { 0u, 31u, 33u })
            {
                auto encoded = GetAssertionCbor(length);
                const auto operationRequest = RequestFor(encoded);

                const HRESULT result = CaptureResult([&]
                {
                    (void)RequestDecoder::DecodeGetAssertion(&operationRequest);
                });

                Assert::IsTrue(FAILED(result));
            }
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
                OptionCase{ true, 0xf5, 0xf4, true, false },
                OptionCase{ true, 0xf4, 0xf4, false, false },
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

        TEST_METHOD(DecodeMakeCredentialRejectsClientDataHashLengthsOutsideThirtyTwoBytes)
        {
            for (const std::size_t length : { 0u, 31u, 33u })
            {
                auto encoded = MakeCredentialCbor(true, 0xf4, 0xf4, length);
                const auto operationRequest = RequestFor(encoded);

                const HRESULT result = CaptureResult([&]
                {
                    (void)RequestDecoder::DecodeMakeCredential(&operationRequest);
                });

                Assert::IsTrue(FAILED(result));
            }
        }
    };

    TEST_CLASS(PluginAuthenticatorEndToEndTests)
    {
    public:
        TEST_METHOD(ValidSignedRequestsReachTheClientAndEncodeBothResponses)
        {
            PluginAuthenticatorHarness makeHarness;
            makeHarness.passkeyClient->makeCredentialResponse.AuthenticatorData = { 0x11, 0x22 };
            SignedOperationRequest makeRequest{ MakeCredentialCbor(true, 0xf5, 0xf5) };
            WEBAUTHN_PLUGIN_OPERATION_RESPONSE makeResponse{};

            Assert::AreEqual(
                S_OK,
                makeHarness.authenticator->MakeCredential(&makeRequest.request, &makeResponse));
            Assert::AreEqual(1U, makeHarness.passkeyClient->makeCredentialCallCount);
            Assert::IsTrue(makeHarness.passkeyClient->lastMakeCredentialRequest.has_value());
            const auto& decodedMakeRequest = *makeHarness.passkeyClient->lastMakeCredentialRequest;
            Assert::AreEqual(std::string{ "example.com" }, decodedMakeRequest.RpId);
            Assert::AreEqual(std::string{ "Example" }, decodedMakeRequest.RpName);
            Assert::IsTrue(decodedMakeRequest.RpIdHash ==
                std::vector<std::uint8_t>(ExampleRpIdHash.begin(), ExampleRpIdHash.end()));
            Assert::IsTrue(decodedMakeRequest.ClientDataHash ==
                std::vector<std::uint8_t>(ClientDataHash.begin(), ClientDataHash.end()));
            Assert::IsTrue(decodedMakeRequest.UserId == std::vector<std::uint8_t>{ 0x01 });
            Assert::AreEqual(std::string{ "alice" }, decodedMakeRequest.UserName);
            Assert::AreEqual(std::string{ "Alice" }, decodedMakeRequest.UserDisplayName);
            Assert::AreEqual(true, decodedMakeRequest.RequireResidentKey);
            Assert::AreEqual(true, decodedMakeRequest.UserVerification);
            Assert::IsTrue(makeHarness.calls == std::vector<std::string>{ "make-credential" });
            Assert::AreEqual(static_cast<DWORD>(3), makeResponse.cbEncodedResponse);
            Assert::IsTrue(std::vector<BYTE>(
                makeResponse.pbEncodedResponse,
                makeResponse.pbEncodedResponse + makeResponse.cbEncodedResponse) ==
                std::vector<BYTE>{ 0x4d, 0x43, 0x01 });

            PluginAuthenticatorHarness assertionHarness;
            assertionHarness.passkeyClient->getAssertionResponse.CredentialId = { 0xa1, 0xa2 };
            assertionHarness.passkeyClient->getAssertionResponse.UserId = { 0xb1 };
            assertionHarness.passkeyClient->getAssertionResponse.AuthenticatorData = { 0x31, 0x32, 0x33 };
            assertionHarness.passkeyClient->getAssertionResponse.Signature = { 0xd1, 0xd2 };
            assertionHarness.passkeyClient->getAssertionResponse.UserName = "alice";
            assertionHarness.passkeyClient->getAssertionResponse.UserDisplayName = "Alice";
            SignedOperationRequest assertionRequest{ GetAssertionCbor() };
            WEBAUTHN_PLUGIN_OPERATION_RESPONSE assertionResponse{};

            Assert::AreEqual(
                S_OK,
                assertionHarness.authenticator->GetAssertion(&assertionRequest.request, &assertionResponse));
            Assert::AreEqual(1U, assertionHarness.passkeyClient->getAssertionCallCount);
            Assert::IsTrue(assertionHarness.passkeyClient->lastGetAssertionRequest.has_value());
            const auto& decodedAssertionRequest = *assertionHarness.passkeyClient->lastGetAssertionRequest;
            Assert::AreEqual(std::string{ "example.com" }, decodedAssertionRequest.RpId);
            Assert::IsTrue(decodedAssertionRequest.RpIdHash ==
                std::vector<std::uint8_t>(ExampleRpIdHash.begin(), ExampleRpIdHash.end()));
            Assert::IsTrue(decodedAssertionRequest.ClientDataHash ==
                std::vector<std::uint8_t>(ClientDataHash.begin(), ClientDataHash.end()));
            Assert::IsTrue(assertionHarness.calls == std::vector<std::string>{ "get-assertion" });
            Assert::AreEqual(static_cast<DWORD>(3), assertionResponse.cbEncodedResponse);
            Assert::IsTrue(std::vector<BYTE>(
                assertionResponse.pbEncodedResponse,
                assertionResponse.pbEncodedResponse + assertionResponse.cbEncodedResponse) ==
                std::vector<BYTE>{ 0x47, 0x41, 0x01 });
        }

        TEST_METHOD(InvalidSignaturesStopBothOperationsAndPreserveExistingResponses)
        {
            for (const bool getAssertion : { false, true })
            {
                for (const bool tamperRequest : { false, true })
                {
                    PluginAuthenticatorHarness harness;
                    SignedOperationRequest request{ getAssertion ? GetAssertionCbor() :
                        MakeCredentialCbor(true, 0xf4, 0xf4) };
                    if (tamperRequest)
                    {
                        request.encodedRequest.front() ^= 0x01;
                    }
                    else
                    {
                        request.signature.front() ^= 0x01;
                    }
                    std::array<BYTE, 1> sentinel{};
                    WEBAUTHN_PLUGIN_OPERATION_RESPONSE response{};
                    SetSentinelResponse(response, sentinel);

                    const HRESULT result = getAssertion
                        ? harness.authenticator->GetAssertion(&request.request, &response)
                        : harness.authenticator->MakeCredential(&request.request, &response);

                    Assert::AreEqual(static_cast<HRESULT>(NTE_BAD_SIGNATURE), result);
                    Assert::IsTrue(harness.calls.empty());
                    Assert::AreEqual(0U, harness.passkeyClient->makeCredentialCallCount);
                    Assert::AreEqual(0U, harness.passkeyClient->getAssertionCallCount);
                    Assert::AreEqual(static_cast<DWORD>(1), response.cbEncodedResponse);
                    Assert::IsTrue(response.pbEncodedResponse == sentinel.data());
                }
            }
        }

        TEST_METHOD(MalformedSignedCborReturnsDecoderErrorAndClearsOnlyGetAssertionResponse)
        {
            for (const bool getAssertion : { false, true })
            {
                PluginAuthenticatorHarness harness;
                SignedOperationRequest request{ { 0xff } };
                std::array<BYTE, 1> sentinel{};
                WEBAUTHN_PLUGIN_OPERATION_RESPONSE response{};
                SetSentinelResponse(response, sentinel);

                const HRESULT result = getAssertion
                    ? harness.authenticator->GetAssertion(&request.request, &response)
                    : harness.authenticator->MakeCredential(&request.request, &response);

                Assert::AreEqual(HRESULT_FROM_WIN32(ERROR_INVALID_DATA), result);
                Assert::IsTrue(harness.calls.empty());
                Assert::AreEqual(0U, harness.passkeyClient->makeCredentialCallCount);
                Assert::AreEqual(0U, harness.passkeyClient->getAssertionCallCount);
                if (getAssertion)
                {
                    AssertResponseIsEmpty(response);
                }
                else
                {
                    Assert::AreEqual(static_cast<DWORD>(1), response.cbEncodedResponse);
                    Assert::IsTrue(response.pbEncodedResponse == sentinel.data());
                }
            }
        }

        TEST_METHOD(ClientFailuresPropagateAndRespectResponseInitialization)
        {
            PluginAuthenticatorHarness makeHarness;
            makeHarness.passkeyClient->makeCredentialFailure = HRESULT_FROM_WIN32(ERROR_ACCESS_DENIED);
            SignedOperationRequest makeRequest{ MakeCredentialCbor(true, 0xf5, 0xf5) };
            std::array<BYTE, 1> sentinel{};
            WEBAUTHN_PLUGIN_OPERATION_RESPONSE makeResponse{};
            SetSentinelResponse(makeResponse, sentinel);

            Assert::AreEqual(
                HRESULT_FROM_WIN32(ERROR_ACCESS_DENIED),
                makeHarness.authenticator->MakeCredential(&makeRequest.request, &makeResponse));
            Assert::AreEqual(1U, makeHarness.passkeyClient->makeCredentialCallCount);
            Assert::IsTrue(makeHarness.passkeyClient->lastMakeCredentialRequest.has_value());
            Assert::AreEqual(static_cast<DWORD>(1), makeResponse.cbEncodedResponse);
            Assert::IsTrue(makeResponse.pbEncodedResponse == sentinel.data());

            PluginAuthenticatorHarness assertionHarness;
            assertionHarness.passkeyClient->getAssertionFailure = HRESULT_FROM_WIN32(ERROR_ACCESS_DENIED);
            SignedOperationRequest assertionRequest{ GetAssertionCbor() };
            std::array<BYTE, 1> assertionSentinel{};
            WEBAUTHN_PLUGIN_OPERATION_RESPONSE assertionResponse{};
            SetSentinelResponse(assertionResponse, assertionSentinel);

            Assert::AreEqual(
                HRESULT_FROM_WIN32(ERROR_ACCESS_DENIED),
                assertionHarness.authenticator->GetAssertion(&assertionRequest.request, &assertionResponse));
            Assert::AreEqual(1U, assertionHarness.passkeyClient->getAssertionCallCount);
            Assert::IsTrue(assertionHarness.passkeyClient->lastGetAssertionRequest.has_value());
            AssertResponseIsEmpty(assertionResponse);
        }

        TEST_METHOD(EncoderFailuresPropagateWithoutPublishingPartialResponses)
        {
            PluginAuthenticatorHarness makeHarness;
            makeHarness.passkeyClient->makeCredentialResponse.AuthenticatorData = { 0xee };
            SignedOperationRequest makeRequest{ MakeCredentialCbor(true, 0xf5, 0xf5) };
            std::array<BYTE, 1> sentinel{};
            WEBAUTHN_PLUGIN_OPERATION_RESPONSE makeResponse{};
            SetSentinelResponse(makeResponse, sentinel);

            Assert::AreEqual(
                E_ACCESSDENIED,
                makeHarness.authenticator->MakeCredential(&makeRequest.request, &makeResponse));
            Assert::AreEqual(1U, makeHarness.passkeyClient->makeCredentialCallCount);
            Assert::AreEqual(static_cast<DWORD>(1), makeResponse.cbEncodedResponse);
            Assert::IsTrue(makeResponse.pbEncodedResponse == sentinel.data());

            PluginAuthenticatorHarness assertionHarness;
            assertionHarness.passkeyClient->getAssertionResponse.CredentialId = { 0xa1, 0xa2 };
            assertionHarness.passkeyClient->getAssertionResponse.UserId = { 0xb1 };
            assertionHarness.passkeyClient->getAssertionResponse.AuthenticatorData = { 0x31, 0x32, 0x33 };
            assertionHarness.passkeyClient->getAssertionResponse.Signature = { 0xee };
            assertionHarness.passkeyClient->getAssertionResponse.UserName = "alice";
            assertionHarness.passkeyClient->getAssertionResponse.UserDisplayName = "Alice";
            SignedOperationRequest assertionRequest{ GetAssertionCbor() };
            std::array<BYTE, 1> assertionSentinel{};
            WEBAUTHN_PLUGIN_OPERATION_RESPONSE assertionResponse{};
            SetSentinelResponse(assertionResponse, assertionSentinel);

            Assert::AreEqual(
                E_ACCESSDENIED,
                assertionHarness.authenticator->GetAssertion(&assertionRequest.request, &assertionResponse));
            Assert::AreEqual(1U, assertionHarness.passkeyClient->getAssertionCallCount);
            AssertResponseIsEmpty(assertionResponse);
        }
    };

}

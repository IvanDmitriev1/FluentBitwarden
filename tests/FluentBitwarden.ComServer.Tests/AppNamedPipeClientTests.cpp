#include "pch.h"
#include "Infrastructure/Ipc/AppNamedPipeClient.h"
#include "Infrastructure/Ipc/IpcProtocol.h"
#include "NamedPipeTestServer.h"
#include "Passkey/Models/PasskeyAssertionMessage.h"
#include "Passkey/Models/PasskeyMakeCredentialMessage.h"
#include "TestBytes.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;
using namespace FluentBitwarden::ComServer::Ipc;
using namespace FluentBitwarden::ComServer::WebAuthn;

namespace FluentBitwarden::ComServer::Tests
{
    namespace
    {
        struct CapturedRequest final
        {
            std::vector<std::byte> Header;
            std::vector<std::byte> Payload;
        };

        CapturedRequest ReadRequest(HANDLE pipe)
        {
            CapturedRequest request;
            request.Header = NamedPipeTestIo::Read(pipe, RequestHeader::Size);
            const auto parsedHeader = RequestHeader::Parse(request.Header);
            request.Payload = NamedPipeTestIo::Read(
                pipe,
                static_cast<std::size_t>(parsedHeader.PayloadLength));
            return request;
        }

        PasskeyGetAssertionRequest MakeAssertionRequest()
        {
            return PasskeyGetAssertionRequest
            {
                .RpId = "rp",
                .RpIdHash = std::vector<std::uint8_t>(32, 0x11),
                .ClientDataHash = std::vector<std::uint8_t>(32, 0x22)
            };
        }

        std::vector<std::byte> GetAssertionRequestPayload()
        {
            auto expected = TestBytes::FromOctets({
                0x03,
                0xfd, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x72, 0x70,
                0x20, 0x00, 0x00, 0x00
            });
            expected.insert(expected.end(), 32, std::byte{ 0x11 });
            expected.insert(expected.end(), { std::byte{ 0x20 }, std::byte{ 0 }, std::byte{ 0 }, std::byte{ 0 } });
            expected.insert(expected.end(), 32, std::byte{ 0x22 });
            return expected;
        }

        std::vector<std::byte> MakeCredentialRequestPayload()
        {
            auto expected = TestBytes::FromOctets({
                0x09,
                0xfe, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x72,
                0xfe, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x6e,
                0x20, 0x00, 0x00, 0x00
            });
            expected.insert(expected.end(), 32, std::byte{ 0x11 });
            expected.insert(expected.end(), { std::byte{ 0x20 }, std::byte{ 0 }, std::byte{ 0 }, std::byte{ 0 } });
            expected.insert(expected.end(), 32, std::byte{ 0x22 });
            expected.insert(expected.end(), { std::byte{ 0x02 }, std::byte{ 0 }, std::byte{ 0 }, std::byte{ 0 }, std::byte{ 0xab }, std::byte{ 0 } });
            expected.insert(expected.end(), {
                std::byte{ 0xfe }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff },
                std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0x75 },
                std::byte{ 0xfe }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff },
                std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0x64 },
                std::byte{ 0x01 }, std::byte{ 0x00 }
            });
            return expected;
        }

        PasskeyMakeCredentialRequest MakeCredentialRequest()
        {
            return PasskeyMakeCredentialRequest
            {
                .RpId = "r",
                .RpName = "n",
                .RpIdHash = std::vector<std::uint8_t>(32, 0x11),
                .ClientDataHash = std::vector<std::uint8_t>(32, 0x22),
                .UserId = { 0xab, 0x00 },
                .UserName = "u",
                .UserDisplayName = "d",
                .RequireResidentKey = true,
                .UserVerification = false
            };
        }

        void WriteSuccessResponse(HANDLE pipe, std::span<const std::byte> payload)
        {
            const auto responseHeader = TestBytes::FromOctets({
                0x04, 0x00, 0x01,
                static_cast<std::uint8_t>(payload.size()), 0x00, 0x00, 0x00
            });
            NamedPipeTestIo::Write(pipe, responseHeader);
            if (!payload.empty())
            {
                const auto firstChunk = std::min<std::size_t>(payload.size(), 2);
                NamedPipeTestIo::Write(pipe, payload.first(firstChunk));
                NamedPipeTestIo::Write(pipe, payload.subspan(firstChunk));
            }
        }

        void ReadAndDiscardRequest(HANDLE pipe)
        {
            (void)ReadRequest(pipe);
        }
    }

    TEST_CLASS(AppNamedPipeClientTests)
    {
    public:
        TEST_METHOD(SendAsyncGetAssertionUsesProductionFramingAndParsesFragmentedResponse)
        {
            const auto responsePayload = TestBytes::FromOctets({
                0x01, 0x06,
                0x01, 0x00, 0x00, 0x00, 0xaa,
                0x02, 0x00, 0x00, 0x00, 0xbb, 0xcc,
                0x03, 0x00, 0x00, 0x00, 0x11, 0x22, 0x33,
                0x01, 0x00, 0x00, 0x00, 0xdd,
                0xfe, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x75,
                0x00, 0x00, 0x00, 0x00
            });
            CapturedRequest captured;
            NamedPipeTestServer server;
            server.Start([&](HANDLE pipe)
            {
                captured = ReadRequest(pipe);
                WriteSuccessResponse(pipe, responsePayload);
            });

            AppNamedPipeClient client{ server.Path() };
            server.WaitForConnection();
            const auto response = client.SendAsync<PasskeyGetAssertionRequest, PasskeyAssertionResponse>(MakeAssertionRequest()).get();
            server.Wait();

            const auto expectedHeader = TestBytes::FromOctets({ 0x04, 0x00, 0x32, 0x00, 0x53, 0x00, 0x00, 0x00 });
            TestBytes::AssertBytesEqual(expectedHeader, captured.Header);
            TestBytes::AssertBytesEqual(GetAssertionRequestPayload(), captured.Payload);
            TestBytes::AssertOctetsEqual(std::array<std::uint8_t, 1>{ 0xaa }, response.CredentialId);
            TestBytes::AssertOctetsEqual(std::array<std::uint8_t, 2>{ 0xbb, 0xcc }, response.UserId);
            TestBytes::AssertOctetsEqual(std::array<std::uint8_t, 3>{ 0x11, 0x22, 0x33 }, response.AuthenticatorData);
            TestBytes::AssertOctetsEqual(std::array<std::uint8_t, 1>{ 0xdd }, response.Signature);
            Assert::IsTrue(response.UserName == "u");
            Assert::IsTrue(response.UserDisplayName.empty());
        }

        TEST_METHOD(SendAsyncMakeCredentialUsesProductionFramingAndParsesResponse)
        {
            const auto responsePayload = TestBytes::FromOctets({ 0x01, 0x01, 0x02, 0x00, 0x00, 0x00, 0x44, 0x55 });
            CapturedRequest captured;
            NamedPipeTestServer server;
            server.Start([&](HANDLE pipe)
            {
                captured = ReadRequest(pipe);
                WriteSuccessResponse(pipe, responsePayload);
            });

            AppNamedPipeClient client{ server.Path() };
            server.WaitForConnection();
            const auto response = client.SendAsync<PasskeyMakeCredentialRequest, PasskeyMakeCredentialResponse>(MakeCredentialRequest()).get();
            server.Wait();

            const auto expectedHeader = TestBytes::FromOctets({ 0x04, 0x00, 0x33, 0x00, 0x75, 0x00, 0x00, 0x00 });
            TestBytes::AssertBytesEqual(expectedHeader, captured.Header);
            TestBytes::AssertBytesEqual(MakeCredentialRequestPayload(), captured.Payload);
            TestBytes::AssertOctetsEqual(std::array<std::uint8_t, 2>{ 0x44, 0x55 }, response.AuthenticatorData);
        }

        TEST_METHOD(SendAsyncRejectsUnsuccessfulMalformedAndTruncatedResponseData)
        {
            const auto unsuccessful = TestBytes::FromOctets({ 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
            AssertRequestFailsWithResponse(unsuccessful, {});

            const auto wrongVersion = TestBytes::FromOctets({ 0x03, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00 });
            AssertRequestFailsWithResponse(wrongVersion, {});

            const auto invalidFlags = TestBytes::FromOctets({ 0x04, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00 });
            AssertRequestFailsWithResponse(invalidFlags, {});

            const auto negativeLength = TestBytes::FromOctets({ 0x04, 0x00, 0x01, 0xff, 0xff, 0xff, 0xff });
            AssertRequestFailsWithResponse(negativeLength, {});

            const auto truncatedHeader = TestBytes::FromOctets({ 0x04, 0x00, 0x01, 0x05, 0x00, 0x00, 0x00 });
            const auto partialPayload = TestBytes::FromOctets({ 0xaa, 0xbb });
            AssertRequestFailsWithResponse(truncatedHeader, partialPayload, true);
        }

    private:
        void AssertRequestFailsWithResponse(
            std::span<const std::byte> responseHeader,
            std::span<const std::byte> responsePayload,
            bool disconnectAfterPayload = false)
        {
            NamedPipeTestServer server;
            server.Start([&](HANDLE pipe)
            {
                ReadAndDiscardRequest(pipe);
                NamedPipeTestIo::Write(pipe, responseHeader);
                NamedPipeTestIo::Write(pipe, responsePayload);
                if (disconnectAfterPayload)
                {
                    server.Disconnect();
                }
            });

            AppNamedPipeClient client{ server.Path() };
            server.WaitForConnection();
            TestBytes::AssertThrowsAny([&]
            {
                (void)client.SendAsync<PasskeyGetAssertionRequest, PasskeyAssertionResponse>(MakeAssertionRequest()).get();
            });
            server.Wait();
        }
    };
}

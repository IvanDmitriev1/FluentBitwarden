#include "pch.h"
#include "Infrastructure/Ipc/AppNamedPipeClient.h"
#include "Passkey/Models/PasskeyAssertionMessage.h"
#include "../Support/PasskeyIpcFixtures.h"
#include "../Support/ScriptedNamedPipe.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;
using namespace FluentBitwarden::ComServer::Ipc;
using namespace FluentBitwarden::ComServer::WebAuthn;

namespace FluentBitwarden::ComServer::Tests
{
    TEST_CLASS(AppNamedPipeClientTests)
    {
    public:
        TEST_METHOD(SendsLiteralVersionFourHeaderAndGoldenPayloadAndReadsFragmentedResponse)
        {
            const auto expectedRequest = PasskeyIpcFixtures::GetAssertionRequestPayload();
            const std::array<std::byte, 8> expectedRequestHeader{
                std::byte{ 0x04 }, std::byte{ 0x00 }, std::byte{ 0x32 }, std::byte{ 0x00 },
                std::byte{ 0x53 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }
            };
            const auto responsePayload = PasskeyIpcFixtures::GetAssertionResponsePayload();
            const std::array<std::byte, 7> responseHeader{
                std::byte{ 0x04 }, std::byte{ 0x00 }, std::byte{ 0x01 },
                std::byte{ 0x26 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }
            };
            std::vector<std::byte> observedHeader;
            std::vector<std::byte> observedPayload;

            ScriptedPipe::Server server([&](HANDLE pipe, HANDLE release)
            {
                observedHeader = ScriptedPipe::ReadExactly(pipe, expectedRequestHeader.size());
                observedPayload = ScriptedPipe::ReadExactly(pipe, expectedRequest.size());
                const std::array<std::size_t, 3> headerFragments{ 2, 1, 4 };
                const std::array<std::size_t, 3> payloadFragments{ 1, 7, responsePayload.size() - 8 };
                ScriptedPipe::WriteFragments(pipe, responseHeader, headerFragments);
                ScriptedPipe::WriteFragments(pipe, responsePayload, payloadFragments);
                ScriptedPipe::WaitForRelease(release);
            });

            AppNamedPipeClient client{ server.Path() };
            const PasskeyAssertionResponse response = client.SendAsync<
                PasskeyGetAssertionRequest,
                PasskeyAssertionResponse>(PasskeyIpcFixtures::GetAssertionRequest()).get();
            server.ReleaseAndJoin();

            Assert::IsTrue(observedHeader == std::vector<std::byte>(expectedRequestHeader.begin(), expectedRequestHeader.end()));
            Assert::IsTrue(observedPayload == expectedRequest);
            Assert::IsTrue(response.CredentialId == std::vector<std::uint8_t>{ 0xaa });
            Assert::IsTrue(response.UserId == std::vector<std::uint8_t>{ 0xbb, 0xcc });
            Assert::IsTrue(response.AuthenticatorData == std::vector<std::uint8_t>{ 0x11, 0x22, 0x33 });
            Assert::IsTrue(response.Signature == std::vector<std::uint8_t>{ 0xdd });
            Assert::AreEqual(std::string{ "u" }, response.UserName);
            Assert::IsTrue(response.UserDisplayName.empty());
        }

        TEST_METHOD(UnsuccessfulResponseMapsToCancelledHresult)
        {
            const auto payload = PasskeyIpcFixtures::GetAssertionResponsePayload();
            const std::array<std::byte, 7> header{
                std::byte{ 0x04 }, std::byte{ 0x00 }, std::byte{ 0x00 },
                std::byte{ 0x26 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }
            };
            ScriptedPipe::Server server([&](HANDLE pipe, HANDLE release)
            {
                ScriptedPipe::WriteExactly(pipe, header);
                ScriptedPipe::WriteExactly(pipe, payload);
                ScriptedPipe::WaitForRelease(release);
            });

            const HRESULT result = SendGetAssertion(server.Path());
            server.ReleaseAndJoin();
            Assert::AreEqual(HRESULT_FROM_WIN32(ERROR_CANCELLED), result);
        }

        TEST_METHOD(InvalidResponseVersionSuccessFlagAndLengthAreRejected)
        {
            const auto validPayload = PasskeyIpcFixtures::GetAssertionResponsePayload();
            constexpr std::string_view versionError{ "Incompatible IPC protocol version." };
            constexpr std::string_view successFlagError{ "Invalid IPC response success flag." };
            constexpr std::string_view lengthError{ "Invalid IPC payload length." };

            const std::array<std::byte, 7> invalidVersion{
                std::byte{ 0x03 }, std::byte{ 0x00 }, std::byte{ 0x01 },
                std::byte{ 0x26 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }
            };
            AssertProtocolError(invalidVersion, validPayload, versionError);

            const std::array<std::byte, 7> invalidSuccessFlag{
                std::byte{ 0x04 }, std::byte{ 0x00 }, std::byte{ 0x02 },
                std::byte{ 0x26 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }
            };
            AssertProtocolError(invalidSuccessFlag, validPayload, successFlagError);

            const std::array<std::byte, 7> negativeLength{
                std::byte{ 0x04 }, std::byte{ 0x00 }, std::byte{ 0x01 },
                std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }
            };
            AssertProtocolError(negativeLength, {}, lengthError);
        }

        TEST_METHOD(TruncatedResponseHeaderAndBodyFail)
        {
            const std::array<std::byte, 3> headerPrefix{
                std::byte{ 0x04 }, std::byte{ 0x00 }, std::byte{ 0x01 }
            };
            const auto requestPayload = PasskeyIpcFixtures::GetAssertionRequestPayload();
            ScriptedPipe::Server truncatedHeader([&](HANDLE pipe, HANDLE)
            {
                ConsumeGetAssertionRequest(pipe, requestPayload.size());
                ScriptedPipe::WriteExactly(pipe, headerPrefix);
            });
            const HRESULT headerResult = SendGetAssertion(truncatedHeader.Path());
            truncatedHeader.ReleaseAndJoin();
            Assert::IsTrue(FAILED(headerResult));

            const std::array<std::byte, 7> completeHeader{
                std::byte{ 0x04 }, std::byte{ 0x00 }, std::byte{ 0x01 },
                std::byte{ 0x03 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }
            };
            const std::array<std::byte, 2> bodyPrefix{ std::byte{ 0xaa }, std::byte{ 0xbb } };
            ScriptedPipe::Server truncatedBody([&](HANDLE pipe, HANDLE)
            {
                ConsumeGetAssertionRequest(pipe, requestPayload.size());
                ScriptedPipe::WriteExactly(pipe, completeHeader);
                ScriptedPipe::WriteExactly(pipe, bodyPrefix);
            });
            const HRESULT bodyResult = SendGetAssertion(truncatedBody.Path());
            truncatedBody.ReleaseAndJoin();
            Assert::IsTrue(FAILED(bodyResult));
        }

    private:
        static void ConsumeGetAssertionRequest(HANDLE pipe, std::size_t payloadLength)
        {
            (void)ScriptedPipe::ReadExactly(pipe, 8);
            (void)ScriptedPipe::ReadExactly(pipe, payloadLength);
        }

        static void AssertProtocolError(
            const std::array<std::byte, 7>& responseHeader,
            std::span<const std::byte> responsePayload,
            std::string_view expectedMessage)
        {
            const auto requestPayload = PasskeyIpcFixtures::GetAssertionRequestPayload();
            std::vector<std::byte> responseBytes(responseHeader.begin(), responseHeader.end());
            responseBytes.insert(responseBytes.end(), responsePayload.begin(), responsePayload.end());
            ScriptedPipe::Server server([&](HANDLE pipe, HANDLE release)
            {
                ConsumeGetAssertionRequest(pipe, requestPayload.size());
                ScriptedPipe::WriteExactly(pipe, responseBytes);
                ScriptedPipe::WaitForRelease(release);
            });

            try
            {
                AppNamedPipeClient client{ server.Path() };
                (void)client.SendAsync<PasskeyGetAssertionRequest, PasskeyAssertionResponse>(
                    PasskeyIpcFixtures::GetAssertionRequest()).get();
                Assert::Fail(L"Expected the response header parser to reject the scripted header.");
            }
            catch (const std::runtime_error& error)
            {
                Assert::AreEqual(std::string{ expectedMessage }, std::string{ error.what() });
            }

            server.ReleaseAndJoin();
        }

        static HRESULT SendGetAssertion(const std::wstring& pipePath)
        {
            return wil::ResultFromException([&]
            {
                AppNamedPipeClient client{ pipePath };
                (void)client.SendAsync<PasskeyGetAssertionRequest, PasskeyAssertionResponse>(
                    PasskeyIpcFixtures::GetAssertionRequest()).get();
            });
        }
    };
}

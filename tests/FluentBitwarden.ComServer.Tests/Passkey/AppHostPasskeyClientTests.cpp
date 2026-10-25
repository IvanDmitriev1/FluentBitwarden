#include "pch.h"
#include "Passkey/Ipc/AppHostPasskeyClient.h"
#include "../Support/PasskeyIpcFixtures.h"
#include "../Support/ScriptedNamedPipe.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;
using namespace FluentBitwarden::ComServer;
using namespace FluentBitwarden::ComServer::WebAuthn;

namespace FluentBitwarden::ComServer::Tests
{
    TEST_CLASS(AppHostPasskeyClientTests)
    {
    public:
        TEST_METHOD(GetAssertionSendsMessageFiftyAndMapsEveryResponseField)
        {
            const auto expectedPayload = PasskeyIpcFixtures::GetAssertionRequestPayload();
            const std::array<std::byte, 8> expectedHeader{
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
                observedHeader = ScriptedPipe::ReadExactly(pipe, expectedHeader.size());
                observedPayload = ScriptedPipe::ReadExactly(pipe, expectedPayload.size());
                ScriptedPipe::WriteExactly(pipe, responseHeader);
                ScriptedPipe::WriteExactly(pipe, responsePayload);
                ScriptedPipe::WaitForRelease(release);
            });

            AppHostPasskeyClient client{ server.Path() };
            const PasskeyAssertionResponse response = client.GetAssertion(
                PasskeyIpcFixtures::GetAssertionRequest()).get();
            server.ReleaseAndJoin();

            Assert::IsTrue(observedHeader == std::vector<std::byte>(expectedHeader.begin(), expectedHeader.end()));
            Assert::IsTrue(observedPayload == expectedPayload);
            Assert::IsTrue(response.CredentialId == std::vector<std::uint8_t>{ 0xaa });
            Assert::IsTrue(response.UserId == std::vector<std::uint8_t>{ 0xbb, 0xcc });
            Assert::IsTrue(response.AuthenticatorData == std::vector<std::uint8_t>{ 0x11, 0x22, 0x33 });
            Assert::IsTrue(response.Signature == std::vector<std::uint8_t>{ 0xdd });
            Assert::AreEqual(std::string{ "u" }, response.UserName);
            Assert::IsTrue(response.UserDisplayName.empty());
        }

        TEST_METHOD(MakeCredentialSendsMessageFiftyOneAndMapsAuthenticatorData)
        {
            const auto expectedPayload = PasskeyIpcFixtures::MakeCredentialRequestPayload();
            const std::array<std::byte, 8> expectedHeader{
                std::byte{ 0x04 }, std::byte{ 0x00 }, std::byte{ 0x33 }, std::byte{ 0x00 },
                std::byte{ 0x75 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }
            };
            const auto responsePayload = PasskeyIpcFixtures::MakeCredentialResponsePayload();
            const std::array<std::byte, 7> responseHeader{
                std::byte{ 0x04 }, std::byte{ 0x00 }, std::byte{ 0x01 },
                std::byte{ 0x08 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }
            };
            std::vector<std::byte> observedHeader;
            std::vector<std::byte> observedPayload;

            ScriptedPipe::Server server([&](HANDLE pipe, HANDLE release)
            {
                observedHeader = ScriptedPipe::ReadExactly(pipe, expectedHeader.size());
                observedPayload = ScriptedPipe::ReadExactly(pipe, expectedPayload.size());
                ScriptedPipe::WriteExactly(pipe, responseHeader);
                ScriptedPipe::WriteExactly(pipe, responsePayload);
                ScriptedPipe::WaitForRelease(release);
            });

            AppHostPasskeyClient client{ server.Path() };
            const PasskeyMakeCredentialResponse response = client.MakeCredential(
                PasskeyIpcFixtures::MakeCredentialRequest()).get();
            server.ReleaseAndJoin();

            Assert::IsTrue(observedHeader == std::vector<std::byte>(expectedHeader.begin(), expectedHeader.end()));
            Assert::IsTrue(observedPayload == expectedPayload);
            Assert::IsTrue(response.AuthenticatorData == std::vector<std::uint8_t>{ 0x44, 0x55 });
        }

        TEST_METHOD(BothOperationsPropagateMalformedResponsePayloadErrors)
        {
            const std::array<std::byte, 1> assertionPayload{ std::byte{ 0x00 } };
            const std::array<std::byte, 7> assertionHeader{
                std::byte{ 0x04 }, std::byte{ 0x00 }, std::byte{ 0x01 },
                std::byte{ 0x01 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }
            };
            const auto assertionRequestPayload = PasskeyIpcFixtures::GetAssertionRequestPayload();
            ScriptedPipe::Server assertionServer([&](HANDLE pipe, HANDLE release)
            {
                ConsumeRequest(pipe, assertionRequestPayload.size());
                ScriptedPipe::WriteExactly(pipe, assertionHeader);
                ScriptedPipe::WriteExactly(pipe, assertionPayload);
                ScriptedPipe::WaitForRelease(release);
            });
            const HRESULT assertionResult = wil::ResultFromException([&]
            {
                AppHostPasskeyClient client{ assertionServer.Path() };
                (void)client.GetAssertion(PasskeyIpcFixtures::GetAssertionRequest()).get();
            });
            assertionServer.ReleaseAndJoin();
            Assert::IsTrue(FAILED(assertionResult));

            const std::array<std::byte, 2> makeCredentialPayload{ std::byte{ 0x01 }, std::byte{ 0x00 } };
            const std::array<std::byte, 7> makeCredentialHeader{
                std::byte{ 0x04 }, std::byte{ 0x00 }, std::byte{ 0x01 },
                std::byte{ 0x02 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }
            };
            const auto makeCredentialRequestPayload = PasskeyIpcFixtures::MakeCredentialRequestPayload();
            ScriptedPipe::Server makeCredentialServer([&](HANDLE pipe, HANDLE release)
            {
                ConsumeRequest(pipe, makeCredentialRequestPayload.size());
                ScriptedPipe::WriteExactly(pipe, makeCredentialHeader);
                ScriptedPipe::WriteExactly(pipe, makeCredentialPayload);
                ScriptedPipe::WaitForRelease(release);
            });
            const HRESULT makeCredentialResult = wil::ResultFromException([&]
            {
                AppHostPasskeyClient client{ makeCredentialServer.Path() };
                (void)client.MakeCredential(PasskeyIpcFixtures::MakeCredentialRequest()).get();
            });
            makeCredentialServer.ReleaseAndJoin();
            Assert::IsTrue(FAILED(makeCredentialResult));
        }

        TEST_METHOD(BothOperationsPropagateUnsuccessfulIpcResponsesAsCancellation)
        {
            const std::array<std::byte, 7> unsuccessfulHeader{
                std::byte{ 0x04 }, std::byte{ 0x00 }, std::byte{ 0x00 },
                std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }
            };
            const auto assertionRequestPayload = PasskeyIpcFixtures::GetAssertionRequestPayload();
            ScriptedPipe::Server assertionServer([&](HANDLE pipe, HANDLE release)
            {
                ConsumeRequest(pipe, assertionRequestPayload.size());
                ScriptedPipe::WriteExactly(pipe, unsuccessfulHeader);
                ScriptedPipe::WaitForRelease(release);
            });
            const HRESULT assertionResult = wil::ResultFromException([&]
            {
                AppHostPasskeyClient client{ assertionServer.Path() };
                (void)client.GetAssertion(PasskeyIpcFixtures::GetAssertionRequest()).get();
            });
            assertionServer.ReleaseAndJoin();
            Assert::AreEqual(HRESULT_FROM_WIN32(ERROR_CANCELLED), assertionResult);

            const auto makeCredentialRequestPayload = PasskeyIpcFixtures::MakeCredentialRequestPayload();
            ScriptedPipe::Server makeCredentialServer([&](HANDLE pipe, HANDLE release)
            {
                ConsumeRequest(pipe, makeCredentialRequestPayload.size());
                ScriptedPipe::WriteExactly(pipe, unsuccessfulHeader);
                ScriptedPipe::WaitForRelease(release);
            });
            const HRESULT makeCredentialResult = wil::ResultFromException([&]
            {
                AppHostPasskeyClient client{ makeCredentialServer.Path() };
                (void)client.MakeCredential(PasskeyIpcFixtures::MakeCredentialRequest()).get();
            });
            makeCredentialServer.ReleaseAndJoin();
            Assert::AreEqual(HRESULT_FROM_WIN32(ERROR_CANCELLED), makeCredentialResult);
        }

        TEST_METHOD(BothOperationsPropagateNamedPipeOpenFailures)
        {
            const std::wstring invalidPipePath{ L"\\\\.\\pipe\\invalid/name" };
            const HRESULT assertionResult = wil::ResultFromException([&]
            {
                AppHostPasskeyClient client{ invalidPipePath };
                (void)client.GetAssertion(PasskeyIpcFixtures::GetAssertionRequest()).get();
            });
            const HRESULT makeCredentialResult = wil::ResultFromException([&]
            {
                AppHostPasskeyClient client{ invalidPipePath };
                (void)client.MakeCredential(PasskeyIpcFixtures::MakeCredentialRequest()).get();
            });

            Assert::AreEqual(HRESULT_FROM_WIN32(ERROR_TIMEOUT), assertionResult);
            Assert::AreEqual(HRESULT_FROM_WIN32(ERROR_TIMEOUT), makeCredentialResult);
        }

    private:
        static void ConsumeRequest(HANDLE pipe, std::size_t payloadLength)
        {
            (void)ScriptedPipe::ReadExactly(pipe, 8);
            (void)ScriptedPipe::ReadExactly(pipe, payloadLength);
        }
    };
}

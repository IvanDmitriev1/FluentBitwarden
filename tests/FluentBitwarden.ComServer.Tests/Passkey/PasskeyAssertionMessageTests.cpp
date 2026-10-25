#include "pch.h"
#include "Infrastructure/Ipc/IpcProtocol.h"
#include "Passkey/Models/PasskeyAssertionMessage.h"
#include "../Support/PasskeyIpcFixtures.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;
using namespace FluentBitwarden::ComServer::WebAuthn;

namespace FluentBitwarden::ComServer::Tests
{
    TEST_CLASS(PasskeyAssertionMessageTests)
    {
    public:
        TEST_METHOD(GetAssertionRequestMatchesTheManagedMessageIdAndGoldenPayload)
        {
            const PasskeyGetAssertionRequest request = PasskeyIpcFixtures::GetAssertionRequest();

            Assert::AreEqual<std::uint16_t>(50, PasskeyGetAssertionRequest::MessageType);
            Assert::IsTrue(request.ToPayload() == PasskeyIpcFixtures::GetAssertionRequestPayload());
        }

        TEST_METHOD(GetAssertionRequiresEachHashToBeExactlyThirtyTwoBytes)
        {
            for (const std::size_t length : { 0u, 31u, 32u, 33u })
            {
                PasskeyGetAssertionRequest rpIdHashCase = PasskeyIpcFixtures::GetAssertionRequest();
                rpIdHashCase.RpIdHash.resize(length, 0x33);
                if (length == 32)
                {
                    Assert::IsTrue(rpIdHashCase.ToPayload() == PasskeyIpcFixtures::GetAssertionRequestPayload());
                }
                else
                {
                    Assert::AreEqual(E_INVALIDARG,
                        wil::ResultFromException([&] { (void)rpIdHashCase.ToPayload(); }));
                }

                PasskeyGetAssertionRequest clientDataHashCase = PasskeyIpcFixtures::GetAssertionRequest();
                clientDataHashCase.ClientDataHash.resize(length, 0x33);
                if (length == 32)
                {
                    Assert::IsTrue(clientDataHashCase.ToPayload() == PasskeyIpcFixtures::GetAssertionRequestPayload());
                }
                else
                {
                    Assert::AreEqual(E_INVALIDARG,
                        wil::ResultFromException([&] { (void)clientDataHashCase.ToPayload(); }));
                }
            }
        }

        TEST_METHOD(AssertionResponseParsesEveryFieldFromTheGoldenPayload)
        {
            const auto payload = PasskeyIpcFixtures::GetAssertionResponsePayload();

            const PasskeyAssertionResponse response = PasskeyAssertionResponse::FromPayload(payload);
            Assert::IsTrue(response.CredentialId == std::vector<std::uint8_t>{ 0xaa });
            Assert::IsTrue(response.UserId == std::vector<std::uint8_t>{ 0xbb, 0xcc });
            Assert::IsTrue(response.AuthenticatorData == std::vector<std::uint8_t>{ 0x11, 0x22, 0x33 });
            Assert::IsTrue(response.Signature == std::vector<std::uint8_t>{ 0xdd });
            Assert::IsTrue(response.UserName == "u");
            Assert::IsTrue(response.UserDisplayName.empty());
        }

        TEST_METHOD(AssertionResponseRejectsMalformedOuterInnerAndFieldBoundaries)
        {
            auto wrongOuterMemberCount = PasskeyIpcFixtures::GetAssertionResponsePayload();
            wrongOuterMemberCount[0] = std::byte{ 0x00 };
            auto wrongInnerMemberCount = PasskeyIpcFixtures::GetAssertionResponsePayload();
            wrongInnerMemberCount[1] = std::byte{ 0x05 };
            const std::array malformedPayloads{
                wrongOuterMemberCount,
                wrongInnerMemberCount,
                std::vector<std::byte>{ std::byte{ 0x01 }, std::byte{ 0x06 }, std::byte{ 0x00 } },
                std::vector<std::byte>{
                    std::byte{ 0x01 }, std::byte{ 0x06 },
                    std::byte{ 0x01 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }
                },
                std::vector<std::byte>{
                    std::byte{ 0x01 }, std::byte{ 0x06 },
                    std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }
                }
            };

            for (const auto& payload : malformedPayloads)
            {
                Assert::ExpectException<std::runtime_error>([&]
                {
                    (void)PasskeyAssertionResponse::FromPayload(payload);
                });
            }

            auto trailingBytes = PasskeyIpcFixtures::GetAssertionResponsePayload();
            trailingBytes.push_back(std::byte{ 0x00 });
            Assert::ExpectException<std::runtime_error>([&]
            {
                (void)PasskeyAssertionResponse::FromPayload(trailingBytes);
            });
        }

    private:
    };
}

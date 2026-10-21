#include "pch.h"
#include "Infrastructure/Ipc/IpcProtocol.h"
#include "Passkey/Models/PasskeyMakeCredentialMessage.h"
#include "../Support/PasskeyIpcFixtures.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;
using namespace FluentBitwarden::ComServer::WebAuthn;

namespace FluentBitwarden::ComServer::Tests
{
    TEST_CLASS(PasskeyMakeCredentialMessageTests)
    {
    public:
        TEST_METHOD(MakeCredentialRequestMatchesTheManagedMessageIdAndGoldenPayload)
        {
            const PasskeyMakeCredentialRequest request = PasskeyIpcFixtures::MakeCredentialRequest();

            Assert::AreEqual<std::uint16_t>(51, PasskeyMakeCredentialRequest::MessageType);
            Assert::IsTrue(request.ToPayload() == PasskeyIpcFixtures::MakeCredentialRequestPayload());
        }

        TEST_METHOD(MakeCredentialRequiresEachHashToBeExactlyThirtyTwoBytes)
        {
            for (const std::size_t length : { 0u, 31u, 32u, 33u })
            {
                PasskeyMakeCredentialRequest rpIdHashCase = PasskeyIpcFixtures::MakeCredentialRequest();
                rpIdHashCase.RpIdHash.resize(length, 0x33);
                if (length == 32)
                {
                    Assert::IsTrue(rpIdHashCase.ToPayload() == PasskeyIpcFixtures::MakeCredentialRequestPayload());
                }
                else
                {
                    Assert::AreEqual(E_INVALIDARG,
                        wil::ResultFromException([&] { (void)rpIdHashCase.ToPayload(); }));
                }

                PasskeyMakeCredentialRequest clientDataHashCase = PasskeyIpcFixtures::MakeCredentialRequest();
                clientDataHashCase.ClientDataHash.resize(length, 0x33);
                if (length == 32)
                {
                    Assert::IsTrue(clientDataHashCase.ToPayload() == PasskeyIpcFixtures::MakeCredentialRequestPayload());
                }
                else
                {
                    Assert::AreEqual(E_INVALIDARG,
                        wil::ResultFromException([&] { (void)clientDataHashCase.ToPayload(); }));
                }
            }
        }

        TEST_METHOD(MakeCredentialResponseParsesAuthenticatorDataFromTheGoldenPayload)
        {
            const auto payload = PasskeyIpcFixtures::MakeCredentialResponsePayload();

            const PasskeyMakeCredentialResponse response = PasskeyMakeCredentialResponse::FromPayload(payload);
            Assert::IsTrue(response.AuthenticatorData == std::vector<std::uint8_t>{ 0x44, 0x55 });
        }

        TEST_METHOD(MakeCredentialResponseRejectsMalformedOuterInnerAndFieldBoundaries)
        {
            auto wrongOuterMemberCount = PasskeyIpcFixtures::MakeCredentialResponsePayload();
            wrongOuterMemberCount[0] = std::byte{ 0x00 };
            auto wrongInnerMemberCount = PasskeyIpcFixtures::MakeCredentialResponsePayload();
            wrongInnerMemberCount[1] = std::byte{ 0x02 };
            const std::array malformedPayloads{
                wrongOuterMemberCount,
                wrongInnerMemberCount,
                std::vector<std::byte>{ std::byte{ 0x01 }, std::byte{ 0x01 }, std::byte{ 0x00 } },
                std::vector<std::byte>{
                    std::byte{ 0x01 }, std::byte{ 0x01 },
                    std::byte{ 0x02 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x44 }
                },
                std::vector<std::byte>{
                    std::byte{ 0x01 }, std::byte{ 0x01 },
                    std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }
                }
            };

            for (const auto& payload : malformedPayloads)
            {
                Assert::ExpectException<std::runtime_error>([&]
                {
                    (void)PasskeyMakeCredentialResponse::FromPayload(payload);
                });
            }

            auto trailingBytes = PasskeyIpcFixtures::MakeCredentialResponsePayload();
            trailingBytes.push_back(std::byte{ 0x00 });
            Assert::ExpectException<std::runtime_error>([&]
            {
                (void)PasskeyMakeCredentialResponse::FromPayload(trailingBytes);
            });
        }

    };
}

#include "pch.h"
#include "Infrastructure/Ipc/IpcProtocol.h"
#include "Passkey/Models/PasskeyMakeCredentialMessage.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;
using namespace FluentBitwarden::ComServer::WebAuthn;

namespace FluentBitwarden::ComServer::Tests
{
    TEST_CLASS(PasskeyMakeCredentialMessageTests)
    {
    public:
        TEST_METHOD(MakeCredentialRequestMatchesTheManagedMessageIdAndGoldenPayload)
        {
            const PasskeyMakeCredentialRequest request = ValidRequest();

            Assert::AreEqual<std::uint16_t>(51, PasskeyMakeCredentialRequest::MessageType);
            Assert::IsTrue(request.ToPayload() == ExpectedRequestPayload());
        }

        TEST_METHOD(MakeCredentialRequiresEachHashToBeExactlyThirtyTwoBytes)
        {
            for (const std::size_t length : { 0u, 31u, 32u, 33u })
            {
                PasskeyMakeCredentialRequest rpIdHashCase = ValidRequest();
                rpIdHashCase.RpIdHash.resize(length, 0x33);
                if (length == 32)
                {
                    Assert::IsTrue(rpIdHashCase.ToPayload() == ExpectedRequestPayload());
                }
                else
                {
                    Assert::AreEqual(E_INVALIDARG,
                        wil::ResultFromException([&] { (void)rpIdHashCase.ToPayload(); }));
                }

                PasskeyMakeCredentialRequest clientDataHashCase = ValidRequest();
                clientDataHashCase.ClientDataHash.resize(length, 0x33);
                if (length == 32)
                {
                    Assert::IsTrue(clientDataHashCase.ToPayload() == ExpectedRequestPayload());
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
            const std::vector<std::byte> payload{
                std::byte{ 0x01 }, std::byte{ 0x01 },
                std::byte{ 0x02 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 },
                std::byte{ 0x44 }, std::byte{ 0x55 }
            };

            const PasskeyMakeCredentialResponse response = PasskeyMakeCredentialResponse::FromPayload(payload);
            Assert::IsTrue(response.AuthenticatorData == std::vector<std::uint8_t>{ 0x44, 0x55 });
        }

    private:
        static PasskeyMakeCredentialRequest ValidRequest()
        {
            return PasskeyMakeCredentialRequest{
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

        static std::vector<std::byte> ExpectedRequestPayload()
        {
            std::vector<std::byte> expected{
                std::byte{ 0x09 },
                std::byte{ 0xfe }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff },
                std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0x72 },
                std::byte{ 0xfe }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff },
                std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0x6e },
                std::byte{ 0x20 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }
            };
            expected.insert(expected.end(), 32, std::byte{ 0x11 });
            expected.insert(expected.end(), {
                std::byte{ 0x20 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }
            });
            expected.insert(expected.end(), 32, std::byte{ 0x22 });
            expected.insert(expected.end(), {
                std::byte{ 0x02 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 },
                std::byte{ 0xab }, std::byte{ 0x00 },
                std::byte{ 0xfe }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff },
                std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0x75 },
                std::byte{ 0xfe }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff },
                std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0x64 },
                std::byte{ 0x01 }, std::byte{ 0x00 }
            });
            return expected;
        }
    };
}

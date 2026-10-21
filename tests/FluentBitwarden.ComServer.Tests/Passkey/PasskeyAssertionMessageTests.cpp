#include "pch.h"
#include "Infrastructure/Ipc/IpcProtocol.h"
#include "Passkey/Models/PasskeyAssertionMessage.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;
using namespace FluentBitwarden::ComServer::WebAuthn;

namespace FluentBitwarden::ComServer::Tests
{
    TEST_CLASS(PasskeyAssertionMessageTests)
    {
    public:
        TEST_METHOD(GetAssertionRequestMatchesTheManagedMessageIdAndGoldenPayload)
        {
            const PasskeyGetAssertionRequest request = ValidRequest();
            const std::vector<std::byte> expected = ExpectedRequestPayload();

            Assert::AreEqual<std::uint16_t>(50, PasskeyGetAssertionRequest::MessageType);
            Assert::IsTrue(request.ToPayload() == expected);
        }

        TEST_METHOD(GetAssertionRequiresEachHashToBeExactlyThirtyTwoBytes)
        {
            for (const std::size_t length : { 0u, 31u, 32u, 33u })
            {
                PasskeyGetAssertionRequest rpIdHashCase = ValidRequest();
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

                PasskeyGetAssertionRequest clientDataHashCase = ValidRequest();
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

        TEST_METHOD(AssertionResponseParsesEveryFieldFromTheGoldenPayload)
        {
            const std::vector<std::byte> payload{
                std::byte{ 0x01 }, std::byte{ 0x06 },
                std::byte{ 0x01 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0xaa },
                std::byte{ 0x02 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0xbb }, std::byte{ 0xcc },
                std::byte{ 0x03 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x11 }, std::byte{ 0x22 }, std::byte{ 0x33 },
                std::byte{ 0x01 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0xdd },
                std::byte{ 0xfe }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff },
                std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0x75 },
                std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }
            };

            const PasskeyAssertionResponse response = PasskeyAssertionResponse::FromPayload(payload);
            Assert::IsTrue(response.CredentialId == std::vector<std::uint8_t>{ 0xaa });
            Assert::IsTrue(response.UserId == std::vector<std::uint8_t>{ 0xbb, 0xcc });
            Assert::IsTrue(response.AuthenticatorData == std::vector<std::uint8_t>{ 0x11, 0x22, 0x33 });
            Assert::IsTrue(response.Signature == std::vector<std::uint8_t>{ 0xdd });
            Assert::IsTrue(response.UserName == "u");
            Assert::IsTrue(response.UserDisplayName.empty());
        }

    private:
        static PasskeyGetAssertionRequest ValidRequest()
        {
            return PasskeyGetAssertionRequest{
                .RpId = "rp",
                .RpIdHash = std::vector<std::uint8_t>(32, 0x11),
                .ClientDataHash = std::vector<std::uint8_t>(32, 0x22)
            };
        }

        static std::vector<std::byte> ExpectedRequestPayload()
        {
            std::vector<std::byte> expected{
                std::byte{ 0x03 },
                std::byte{ 0xfd }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff },
                std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff },
                std::byte{ 0x72 }, std::byte{ 0x70 },
                std::byte{ 0x20 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }
            };
            expected.insert(expected.end(), 32, std::byte{ 0x11 });
            expected.insert(expected.end(), {
                std::byte{ 0x20 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }
            });
            expected.insert(expected.end(), 32, std::byte{ 0x22 });
            return expected;
        }
    };
}

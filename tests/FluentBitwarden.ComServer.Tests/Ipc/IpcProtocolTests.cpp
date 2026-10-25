#include "pch.h"
#include "Infrastructure/Ipc/IpcProtocol.h"
#include <algorithm>

using namespace Microsoft::VisualStudio::CppUnitTestFramework;
using namespace FluentBitwarden::ComServer::Ipc;

namespace FluentBitwarden::ComServer::Tests
{
    TEST_CLASS(IpcProtocolTests)
    {
    public:
        TEST_METHOD(RequestHeaderWritesAndParsesExactVersionFourBytes)
        {
            const std::vector<std::byte> expected{
                std::byte{ 0x04 }, std::byte{ 0x00 },
                std::byte{ 0x32 }, std::byte{ 0x00 },
                std::byte{ 0x53 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }
            };

            const RequestHeader parsed = RequestHeader::Parse(expected);
            Assert::AreEqual<std::uint16_t>(50, parsed.MessageType);
            Assert::AreEqual<std::int32_t>(83, parsed.PayloadLength);
            Assert::IsTrue(std::ranges::equal(expected, RequestHeader{ .MessageType = 50, .PayloadLength = 83 }.Write()));
        }

        TEST_METHOD(RequestHeaderAcceptsZeroAndMaximumSignedPayloadLengths)
        {
            const std::vector<std::byte> zeroLength{
                std::byte{ 0x04 }, std::byte{ 0x00 }, std::byte{ 0x32 }, std::byte{ 0x00 },
                std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }
            };
            const std::vector<std::byte> maximumLength{
                std::byte{ 0x04 }, std::byte{ 0x00 }, std::byte{ 0x32 }, std::byte{ 0x00 },
                std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0x7f }
            };

            Assert::AreEqual<std::int32_t>(0, RequestHeader::Parse(zeroLength).PayloadLength);
            Assert::AreEqual<std::int32_t>(INT32_MAX, RequestHeader::Parse(maximumLength).PayloadLength);
            Assert::IsTrue(std::ranges::equal(zeroLength, RequestHeader{ .MessageType = 50, .PayloadLength = 0 }.Write()));
            Assert::IsTrue(std::ranges::equal(maximumLength, RequestHeader{ .MessageType = 50, .PayloadLength = INT32_MAX }.Write()));
        }

        TEST_METHOD(RequestHeaderRejectsWrongSizesAndVersions)
        {
            const std::array<std::byte, 7> shortHeader{};
            const std::array<std::byte, 9> longHeader{};
            const std::array<std::byte, 8> wrongVersion{
                std::byte{ 0x03 }, std::byte{ 0x00 }, std::byte{ 0x32 }, std::byte{ 0x00 },
                std::byte{ 0x53 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }
            };

            Assert::ExpectException<std::invalid_argument>([&] { (void)RequestHeader::Parse(shortHeader); });
            Assert::ExpectException<std::invalid_argument>([&] { (void)RequestHeader::Parse(longHeader); });
            Assert::ExpectException<std::runtime_error>([&] { (void)RequestHeader::Parse(wrongVersion); });
        }

        TEST_METHOD(RequestHeaderRejectsNegativePayloadLengthWhenReadingOrWriting)
        {
            const std::array<std::byte, 8> negativeLength{
                std::byte{ 0x04 }, std::byte{ 0x00 }, std::byte{ 0x32 }, std::byte{ 0x00 },
                std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }
            };

            Assert::ExpectException<std::runtime_error>([&] { (void)RequestHeader::Parse(negativeLength); });
            Assert::ExpectException<std::runtime_error>([]
            {
                (void)RequestHeader{ .MessageType = 50, .PayloadLength = -1 }.Write();
            });
        }

        TEST_METHOD(ResponseHeaderParsesBothSuccessFlagsAndZeroOrMaximumLengths)
        {
            const std::array<std::byte, 7> unsuccessfulEmpty{
                std::byte{ 0x04 }, std::byte{ 0x00 }, std::byte{ 0x00 },
                std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }
            };
            const std::array<std::byte, 7> successfulPayload{
                std::byte{ 0x04 }, std::byte{ 0x00 }, std::byte{ 0x01 },
                std::byte{ 0x03 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }
            };
            const std::array<std::byte, 7> successfulMaximumLength{
                std::byte{ 0x04 }, std::byte{ 0x00 }, std::byte{ 0x01 },
                std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0x7f }
            };

            const ResponseHeader unsuccessful = ResponseHeader::Parse(unsuccessfulEmpty);
            Assert::IsFalse(unsuccessful.IsSuccessful);
            Assert::AreEqual<std::int32_t>(0, unsuccessful.PayloadLength);

            const ResponseHeader successful = ResponseHeader::Parse(successfulPayload);
            Assert::IsTrue(successful.IsSuccessful);
            Assert::AreEqual<std::int32_t>(3, successful.PayloadLength);

            const ResponseHeader maximum = ResponseHeader::Parse(successfulMaximumLength);
            Assert::AreEqual<std::int32_t>(INT32_MAX, maximum.PayloadLength);
        }

        TEST_METHOD(ResponseHeaderRejectsWrongSizesVersionsAndSuccessFlags)
        {
            const std::array<std::byte, 6> shortHeader{};
            const std::array<std::byte, 8> longHeader{};
            const std::array<std::byte, 7> wrongVersion{
                std::byte{ 0x03 }, std::byte{ 0x00 }, std::byte{ 0x01 },
                std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }
            };
            const std::array<std::byte, 7> invalidFlagTwo{
                std::byte{ 0x04 }, std::byte{ 0x00 }, std::byte{ 0x02 },
                std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }
            };
            const std::array<std::byte, 7> invalidFlagMaximum{
                std::byte{ 0x04 }, std::byte{ 0x00 }, std::byte{ 0xff },
                std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }
            };

            Assert::ExpectException<std::invalid_argument>([&] { (void)ResponseHeader::Parse(shortHeader); });
            Assert::ExpectException<std::invalid_argument>([&] { (void)ResponseHeader::Parse(longHeader); });
            Assert::ExpectException<std::runtime_error>([&] { (void)ResponseHeader::Parse(wrongVersion); });
            Assert::ExpectException<std::runtime_error>([&] { (void)ResponseHeader::Parse(invalidFlagTwo); });
            Assert::ExpectException<std::runtime_error>([&] { (void)ResponseHeader::Parse(invalidFlagMaximum); });
        }

        TEST_METHOD(ResponseHeaderRejectsNegativePayloadLength)
        {
            const std::array<std::byte, 7> negativeLength{
                std::byte{ 0x04 }, std::byte{ 0x00 }, std::byte{ 0x01 },
                std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }
            };

            Assert::ExpectException<std::runtime_error>([&] { (void)ResponseHeader::Parse(negativeLength); });
        }
    };
}

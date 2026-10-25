#include "pch.h"
#include "Infrastructure/Ipc/IpcProtocol.h"
#include "Infrastructure/Ipc/IpcBinary.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;
using namespace FluentBitwarden::ComServer::Ipc;

namespace FluentBitwarden::ComServer::Tests
{
    TEST_CLASS(IpcBinaryTests)
    {
    public:
        TEST_METHOD(LittleEndianIntegerReadAndWriteUseExpectedByteOrder)
        {
            const std::array<std::byte, 4> unsignedBytes{
                std::byte{ 0x12 }, std::byte{ 0x34 }, std::byte{ 0x56 }, std::byte{ 0x78 }
            };
            Assert::AreEqual<std::uint32_t>(0x78563412, Binary::ReadLe<std::uint32_t>(unsignedBytes));

            std::array<std::byte, 4> writtenUnsigned{};
            Binary::WriteLe<std::uint32_t>(writtenUnsigned, 0x78563412);
            Assert::IsTrue(writtenUnsigned == unsignedBytes);

            const std::array<std::byte, 4> negativeTwoBytes{
                std::byte{ 0xfe }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }
            };
            Assert::AreEqual<std::int32_t>(-2, Binary::ReadLe<std::int32_t>(negativeTwoBytes));
            std::array<std::byte, 4> writtenNegativeTwo{};
            Binary::WriteLe<std::int32_t>(writtenNegativeTwo, -2);
            Assert::IsTrue(writtenNegativeTwo == negativeTwoBytes);
        }

        TEST_METHOD(LittleEndianIntegerReadAndWriteRejectWrongSizes)
        {
            const std::array<std::byte, 3> shortBytes{};
            std::array<std::byte, 3> shortOutput{};

            Assert::ExpectException<std::invalid_argument>([&] { (void)Binary::ReadLe<std::uint32_t>(shortBytes); });
            Assert::ExpectException<std::invalid_argument>([&] { Binary::WriteLe<std::uint32_t>(shortOutput, 1); });
        }

        TEST_METHOD(PayloadWriterAndReaderPreserveEmptyAndUtf8Strings)
        {
            const std::string utf8{ "\xc3\xa9", 2 };
            const std::vector<std::byte> expected{
                std::byte{ 0x02 },
                std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 },
                std::byte{ 0xfd }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff },
                std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff },
                std::byte{ 0xc3 }, std::byte{ 0xa9 }
            };

            Binary::PayloadWriter writer;
            writer.WriteObjectHeader(2);
            writer.WriteString("");
            writer.WriteString(utf8);
            Assert::IsTrue(std::move(writer).Take() == expected);

            Binary::PayloadReader reader{ expected };
            reader.ReadObjectHeader(2);
            Assert::IsTrue(reader.ReadString().empty());
            Assert::IsTrue(reader.ReadString() == utf8);
            reader.EnsureConsumed();
        }

        TEST_METHOD(PayloadWriterAndReaderPreserveByteArrays)
        {
            const std::vector<std::byte> expected{
                std::byte{ 0x01 },
                std::byte{ 0x03 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 },
                std::byte{ 0x11 }, std::byte{ 0x22 }, std::byte{ 0xff }
            };
            const std::array<std::uint8_t, 3> value{ 0x11, 0x22, 0xff };

            Binary::PayloadWriter writer;
            writer.WriteObjectHeader(1);
            writer.WriteBytes(value);
            Assert::IsTrue(std::move(writer).Take() == expected);

            Binary::PayloadReader reader{ expected };
            reader.ReadObjectHeader(1);
            Assert::IsTrue(reader.ReadBytes() == std::vector<std::uint8_t>{ 0x11, 0x22, 0xff });
            reader.EnsureConsumed();
        }

        TEST_METHOD(PayloadReaderRejectsWrongMemberCountsAndTruncatedFields)
        {
            const std::vector<std::byte> wrongMemberCount{ std::byte{ 0x01 } };
            const std::vector<std::byte> truncatedObject{};
            const std::vector<std::byte> truncatedBytes{
                std::byte{ 0x02 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0xaa }
            };
            const std::vector<std::byte> truncatedString{
                std::byte{ 0xfe }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff },
                std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }
            };

            Assert::ExpectException<std::runtime_error>([&]
            {
                Binary::PayloadReader reader{ wrongMemberCount };
                reader.ReadObjectHeader(2);
            });
            Assert::ExpectException<std::runtime_error>([&]
            {
                Binary::PayloadReader reader{ truncatedObject };
                reader.ReadObjectHeader(1);
            });
            Assert::ExpectException<std::runtime_error>([&]
            {
                Binary::PayloadReader reader{ truncatedBytes };
                (void)reader.ReadBytes();
            });
            Assert::ExpectException<std::runtime_error>([&]
            {
                Binary::PayloadReader reader{ truncatedString };
                (void)reader.ReadString();
            });
        }

        TEST_METHOD(PayloadReaderRejectsNegativeCollectionsAndPositiveStringLengths)
        {
            const std::vector<std::byte> negativeCollection{
                std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }, std::byte{ 0xff }
            };
            const std::vector<std::byte> positiveStringLength{
                std::byte{ 0x01 }, std::byte{ 0x00 }, std::byte{ 0x00 }, std::byte{ 0x00 }
            };

            Assert::ExpectException<std::runtime_error>([&]
            {
                Binary::PayloadReader reader{ negativeCollection };
                (void)reader.ReadBytes();
            });
            Assert::ExpectException<std::runtime_error>([&]
            {
                Binary::PayloadReader reader{ positiveStringLength };
                (void)reader.ReadString();
            });
        }

        TEST_METHOD(PayloadReaderEnsureConsumedRejectsTrailingBytes)
        {
            const std::vector<std::byte> payload{ std::byte{ 0x01 }, std::byte{ 0xaa } };

            Assert::ExpectException<std::runtime_error>([&]
            {
                Binary::PayloadReader reader{ payload };
                reader.ReadObjectHeader(1);
                reader.EnsureConsumed();
            });
        }
    };
}

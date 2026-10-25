#include "pch.h"
#include "Infrastructure/Cbor/CborWriter.h"

#include <array>
#include <cstdint>
#include <limits>
#include <span>
#include <string>
#include <string_view>
#include <vector>

using namespace Microsoft::VisualStudio::CppUnitTestFramework;
using namespace FluentBitwarden::ComServer;

namespace FluentBitwarden::ComServer::Tests
{
    namespace
    {
        struct NumericBoundaryCase
        {
            std::uint64_t value;
            std::array<std::uint8_t, 3> initialBytes;
            std::array<std::uint8_t, 8> followingBytes;
            std::size_t followingByteCount;
            const wchar_t* description;
        };

        constexpr std::array NumericBoundaryCases{
            NumericBoundaryCase{ 0, { 0x00, 0xa0, 0x80 }, {}, 0, L"zero" },
            NumericBoundaryCase{ 23, { 0x17, 0xb7, 0x97 }, {}, 0, L"23" },
            NumericBoundaryCase{ 24, { 0x18, 0xb8, 0x98 }, { 0x18 }, 1, L"24" },
            NumericBoundaryCase{ 25, { 0x18, 0xb8, 0x98 }, { 0x19 }, 1, L"25" },
            NumericBoundaryCase{ 255, { 0x18, 0xb8, 0x98 }, { 0xff }, 1, L"255" },
            NumericBoundaryCase{ 256, { 0x19, 0xb9, 0x99 }, { 0x01, 0x00 }, 2, L"256" },
            NumericBoundaryCase{ 257, { 0x19, 0xb9, 0x99 }, { 0x01, 0x01 }, 2, L"257" },
            NumericBoundaryCase{ 65535, { 0x19, 0xb9, 0x99 }, { 0xff, 0xff }, 2, L"65535" },
            NumericBoundaryCase{ 65536, { 0x1a, 0xba, 0x9a }, { 0x00, 0x01, 0x00, 0x00 }, 4, L"65536" },
            NumericBoundaryCase{ 65537, { 0x1a, 0xba, 0x9a }, { 0x00, 0x01, 0x00, 0x01 }, 4, L"65537" },
            NumericBoundaryCase{ 4294967295ULL, { 0x1a, 0xba, 0x9a }, { 0xff, 0xff, 0xff, 0xff }, 4, L"4294967295" },
            NumericBoundaryCase{ 4294967296ULL, { 0x1b, 0xbb, 0x9b }, { 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00 }, 8, L"4294967296" },
            NumericBoundaryCase{ 4294967297ULL, { 0x1b, 0xbb, 0x9b }, { 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01 }, 8, L"4294967297" }
        };

        std::vector<std::uint8_t> ExpectedHeader(const NumericBoundaryCase& testCase, std::size_t encoding)
        {
            std::vector<std::uint8_t> expected{ testCase.initialBytes[encoding] };
            expected.insert(
                expected.end(),
                testCase.followingBytes.begin(),
                testCase.followingBytes.begin() + testCase.followingByteCount);
            return expected;
        }

        struct NegativeBoundaryCase
        {
            std::int64_t value;
            std::vector<std::uint8_t> expected;
            const wchar_t* description;
        };

        struct PayloadBoundaryCase
        {
            std::size_t length;
            std::array<std::uint8_t, 3> textPrefix;
            std::size_t textPrefixLength;
            std::array<std::uint8_t, 3> bytesPrefix;
            std::size_t bytesPrefixLength;
            const wchar_t* description;
        };
    }

    TEST_CLASS(CborWriterTests)
    {
    public:
        TEST_METHOD(UnsignedIntegerUsesCanonicalWidthsAroundEveryBoundary)
        {
            for (const auto& testCase : NumericBoundaryCases)
            {
                CborWriter writer;
                writer.WriteUnsigned(testCase.value);
                Assert::IsTrue(writer.Finish() == ExpectedHeader(testCase, 0), testCase.description);
            }
        }

        TEST_METHOD(MapAndArrayHeadersUseCanonicalWidthsAroundEveryBoundary)
        {
            for (const auto& testCase : NumericBoundaryCases)
            {
                CborWriter mapWriter;
                mapWriter.WriteMap(static_cast<std::size_t>(testCase.value));
                Assert::IsTrue(mapWriter.Finish() == ExpectedHeader(testCase, 1), testCase.description);

                CborWriter arrayWriter;
                arrayWriter.WriteArrayHeader(static_cast<std::size_t>(testCase.value));
                Assert::IsTrue(arrayWriter.Finish() == ExpectedHeader(testCase, 2), testCase.description);
            }
        }

        TEST_METHOD(SignedIntegerUsesCanonicalNegativeBoundariesAndHandlesInt64Minimum)
        {
            const std::array cases{
                NegativeBoundaryCase{ 0, { 0x00 }, L"zero" },
                NegativeBoundaryCase{ 24, { 0x18, 0x18 }, L"positive 24" },
                NegativeBoundaryCase{ -1, { 0x20 }, L"negative one" },
                NegativeBoundaryCase{ -24, { 0x37 }, L"negative 24" },
                NegativeBoundaryCase{ -25, { 0x38, 0x18 }, L"negative 25" },
                NegativeBoundaryCase{ -26, { 0x38, 0x19 }, L"negative 26" },
                NegativeBoundaryCase{ -256, { 0x38, 0xff }, L"negative 256" },
                NegativeBoundaryCase{ -257, { 0x39, 0x01, 0x00 }, L"negative 257" },
                NegativeBoundaryCase{ -258, { 0x39, 0x01, 0x01 }, L"negative 258" },
                NegativeBoundaryCase{ -65536, { 0x39, 0xff, 0xff }, L"negative 65536" },
                NegativeBoundaryCase{ -65537, { 0x3a, 0x00, 0x01, 0x00, 0x00 }, L"negative 65537" },
                NegativeBoundaryCase{ -65538, { 0x3a, 0x00, 0x01, 0x00, 0x01 }, L"negative 65538" },
                NegativeBoundaryCase{ -4294967296LL, { 0x3a, 0xff, 0xff, 0xff, 0xff }, L"negative 4294967296" },
                NegativeBoundaryCase{ -4294967297LL, { 0x3b, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00 }, L"negative 4294967297" },
                NegativeBoundaryCase{ -4294967298LL, { 0x3b, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01 }, L"negative 4294967298" },
                NegativeBoundaryCase{ std::numeric_limits<std::int64_t>::min(), { 0x3b, 0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, L"INT64_MIN" }
            };

            for (const auto& testCase : cases)
            {
                CborWriter writer;
                writer.WriteInteger(testCase.value);
                Assert::IsTrue(writer.Finish() == testCase.expected, testCase.description);
            }
        }

        TEST_METHOD(BooleanValuesUseTheirSimpleValueBytes)
        {
            CborWriter writer;
            writer.WriteBool(false);
            writer.WriteBool(true);

            const std::vector<std::uint8_t> expected{ 0xf4, 0xf5 };
            Assert::IsTrue(writer.Finish() == expected);
        }

        TEST_METHOD(TextAndByteStringsUseLiteralHeadersAroundTwentyFourAndTwoHundredFiftySix)
        {
            const std::array cases{
                PayloadBoundaryCase{ 0, { 0x60 }, 1, { 0x40 }, 1, L"empty" },
                PayloadBoundaryCase{ 23, { 0x77 }, 1, { 0x57 }, 1, L"23" },
                PayloadBoundaryCase{ 24, { 0x78, 0x18 }, 2, { 0x58, 0x18 }, 2, L"24" },
                PayloadBoundaryCase{ 25, { 0x78, 0x19 }, 2, { 0x58, 0x19 }, 2, L"25" },
                PayloadBoundaryCase{ 255, { 0x78, 0xff }, 2, { 0x58, 0xff }, 2, L"255" },
                PayloadBoundaryCase{ 256, { 0x79, 0x01, 0x00 }, 3, { 0x59, 0x01, 0x00 }, 3, L"256" },
                PayloadBoundaryCase{ 257, { 0x79, 0x01, 0x01 }, 3, { 0x59, 0x01, 0x01 }, 3, L"257" }
            };

            for (const auto& testCase : cases)
            {
                const std::string text(testCase.length, 'x');
                std::vector<std::uint8_t> expectedText(
                    testCase.textPrefix.begin(),
                    testCase.textPrefix.begin() + testCase.textPrefixLength);
                expectedText.insert(expectedText.end(), text.begin(), text.end());
                CborWriter textWriter;
                textWriter.WriteText(text);
                Assert::IsTrue(textWriter.Finish() == expectedText, testCase.description);

                const std::vector<std::uint8_t> bytes(testCase.length, 0xa5);
                std::vector<std::uint8_t> expectedBytes(
                    testCase.bytesPrefix.begin(),
                    testCase.bytesPrefix.begin() + testCase.bytesPrefixLength);
                expectedBytes.insert(expectedBytes.end(), bytes.begin(), bytes.end());
                CborWriter bytesWriter;
                bytesWriter.WriteBytes(bytes);
                Assert::IsTrue(bytesWriter.Finish() == expectedBytes, testCase.description);
            }
        }

        TEST_METHOD(KeyWritesTheSameUnsignedEncoding)
        {
            CborWriter writer;
            writer.WriteKey(24);

            const std::vector<std::uint8_t> expected{ 0x18, 0x18 };
            Assert::IsTrue(writer.Finish() == expected);
        }

        TEST_METHOD(TextArrayWritesEmptyAndPopulatedArrays)
        {
            CborWriter emptyWriter;
            emptyWriter.WriteTextArray(std::span<const std::string_view>{});
            Assert::IsTrue(emptyWriter.Finish() == std::vector<std::uint8_t>{ 0x80 });

            const std::array<std::string_view, 2> values{ "a", "bc" };
            CborWriter populatedWriter;
            populatedWriter.WriteTextArray(std::span{ values });

            const std::vector<std::uint8_t> expected{ 0x82, 0x61, 0x61, 0x62, 0x62, 0x63 };
            Assert::IsTrue(populatedWriter.Finish() == expected);
        }

        TEST_METHOD(FinishReturnsTheEncodedByteSequence)
        {
            CborWriter writer;
            writer.WriteUnsigned(24);
            writer.WriteBool(true);
            writer.WriteText("x");

            const std::vector<std::uint8_t> expected{ 0x18, 0x18, 0xf5, 0x61, 0x78 };
            Assert::IsTrue(writer.Finish() == expected);
        }
    };
}

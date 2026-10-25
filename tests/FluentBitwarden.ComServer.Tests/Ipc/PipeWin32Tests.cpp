#include "pch.h"
#include "Infrastructure/Ipc/PipeWin32.h"
#include "../Support/ScriptedNamedPipe.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;
using namespace FluentBitwarden::ComServer::Ipc;

namespace FluentBitwarden::ComServer::Tests
{
    TEST_CLASS(PipeWin32Tests)
    {
    public:
        TEST_METHOD(OpenOverlappedPipeConnectsToAByteModeServer)
        {
            ScriptedPipe::Server server([](HANDLE, HANDLE release)
            {
                ScriptedPipe::WaitForRelease(release);
            });

            const auto client = PipeWin32::OpenOverlappedPipe(server.Path(), std::chrono::seconds{ 1 });
            Assert::IsTrue(client.get() != INVALID_HANDLE_VALUE);
            server.ReleaseAndJoin();
        }

        TEST_METHOD(OpenOverlappedPipeTimesOutWhenTheServerIsMissing)
        {
            const std::wstring missingPipe = ScriptedPipe::NewPipePath();
            const HRESULT result = wil::ResultFromException([&]
            {
                (void)PipeWin32::OpenOverlappedPipe(missingPipe, std::chrono::milliseconds{ 25 });
            });

            Assert::AreEqual(HRESULT_FROM_WIN32(ERROR_TIMEOUT), result);
        }

        TEST_METHOD(ReadAndWriteRejectNullAndInvalidHandles)
        {
            struct HandleCase
            {
                HANDLE Value;
                HRESULT Expected;
            };
            const std::array cases{
                HandleCase{ nullptr, E_HANDLE },
                HandleCase{ INVALID_HANDLE_VALUE, E_INVALIDARG }
            };

            for (const auto& testCase : cases)
            {
                const HRESULT readResult = wil::ResultFromException([&]
                {
                    (void)PipeWin32::ReadExactly(testCase.Value, 0).get();
                });
                const HRESULT writeResult = wil::ResultFromException([&]
                {
                    (void)PipeWin32::WriteExactly(testCase.Value, {}).get();
                });

                Assert::AreEqual(testCase.Expected, readResult);
                Assert::AreEqual(testCase.Expected, writeResult);
            }
        }

        TEST_METHOD(ZeroLengthReadAndWriteCompleteOnAConnectedPipe)
        {
            ScriptedPipe::Server server([](HANDLE, HANDLE release)
            {
                ScriptedPipe::WaitForRelease(release);
            });
            const auto client = PipeWin32::OpenOverlappedPipe(server.Path(), std::chrono::seconds{ 1 });

            Assert::IsTrue(PipeWin32::ReadExactly(client.get(), 0).get().empty());
            PipeWin32::WriteExactly(client.get(), {}).get();
            server.ReleaseAndJoin();
        }

        TEST_METHOD(ReadExactlyReturnsTheBytesSentInSeparateFragments)
        {
            const std::vector<std::byte> expected{
                std::byte{ 0x10 }, std::byte{ 0x20 }, std::byte{ 0x30 }, std::byte{ 0x40 }, std::byte{ 0x50 }, std::byte{ 0x60 }, std::byte{ 0x70 }
            };
            const std::array<std::size_t, 3> fragments{ 1, 2, 4 };
            ScriptedPipe::Server server([&](HANDLE pipe, HANDLE release)
            {
                ScriptedPipe::WriteFragments(pipe, expected, fragments);
                ScriptedPipe::WaitForRelease(release);
            });
            const auto client = PipeWin32::OpenOverlappedPipe(server.Path(), std::chrono::seconds{ 1 });

            const auto actual = PipeWin32::ReadExactly(client.get(), expected.size()).get();
            server.ReleaseAndJoin();
            Assert::IsTrue(actual == expected);
        }

        TEST_METHOD(LargeTransfersPreserveEveryByteInBothDirections)
        {
            std::vector<std::byte> expected(1024 * 1024);
            for (std::size_t index = 0; index < expected.size(); ++index)
                expected[index] = static_cast<std::byte>((index * 31) & 0xff);

            std::vector<std::byte> received;
            ScriptedPipe::Server server([&](HANDLE pipe, HANDLE release)
            {
                received = ScriptedPipe::ReadExactly(pipe, expected.size());
                ScriptedPipe::WriteExactly(pipe, received);
                ScriptedPipe::WaitForRelease(release);
            });
            const auto client = PipeWin32::OpenOverlappedPipe(server.Path(), std::chrono::seconds{ 1 });

            PipeWin32::WriteExactly(client.get(), expected).get();
            const auto actual = PipeWin32::ReadExactly(client.get(), expected.size()).get();
            server.ReleaseAndJoin();
            Assert::IsTrue(received == expected);
            Assert::IsTrue(actual == expected);
        }

        TEST_METHOD(ReadExactlyFailsWhenThePeerClosesBeforeTheRequestedBytesArrive)
        {
            const std::array<std::byte, 2> prefix{ std::byte{ 0x11 }, std::byte{ 0x22 } };
            ScriptedPipe::Server server([&](HANDLE pipe, HANDLE)
            {
                ScriptedPipe::WriteExactly(pipe, prefix);
            });
            const auto client = PipeWin32::OpenOverlappedPipe(server.Path(), std::chrono::seconds{ 1 });

            const HRESULT result = wil::ResultFromException([&]
            {
                (void)PipeWin32::ReadExactly(client.get(), 5).get();
            });
            server.ReleaseAndJoin();

            Assert::IsTrue(FAILED(result));
        }
    };
}

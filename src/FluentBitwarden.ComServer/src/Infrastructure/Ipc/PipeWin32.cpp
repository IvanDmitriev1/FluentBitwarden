#include "pch.h"
#include "Infrastructure/Ipc/PipeWin32.h"

namespace FluentBitwarden::ComServer::Ipc::PipeWin32
{

    namespace
    {
        template <typename StartOperation>
        concept OverlappedStarter = requires(StartOperation operation, OVERLAPPED & overlapped)
        {
            { operation(overlapped) } -> std::same_as<BOOL>;
        };

        void ValidatePipeHandle(HANDLE pipe)
        {
            THROW_HR_IF_NULL(E_HANDLE, pipe);
            THROW_HR_IF(E_INVALIDARG, pipe == INVALID_HANDLE_VALUE);
        }

        template <OverlappedStarter StartOperation>
        wil::task<std::uint32_t> AwaitOverlappedAsync(HANDLE pipe, StartOperation operation)
        {
            wil::unique_handle event{ ::CreateEventW(nullptr, TRUE, FALSE, nullptr) };
            THROW_LAST_ERROR_IF(!event);

            OVERLAPPED overlapped{};
            overlapped.hEvent = event.get();

            const BOOL completedSynchronously = operation(overlapped);

            if (!completedSynchronously)
            {
                const DWORD error = ::GetLastError();
                THROW_WIN32_IF(error, error != ERROR_IO_PENDING);
                co_await winrt::resume_on_signal(event.get());
            }

            DWORD transferred{};
            THROW_IF_WIN32_BOOL_FALSE(::GetOverlappedResult(pipe, &overlapped, &transferred, FALSE));

            co_return static_cast<std::uint32_t>(transferred);
        }
    }

    wil::unique_hfile OpenOverlappedPipe(const std::wstring& pipePath, TimeSpan timeout)
    {
        const auto deadline = std::chrono::steady_clock::now() + std::chrono::duration_cast<std::chrono::nanoseconds>(timeout);

        while (true)
        {
            HANDLE rawHandle = ::CreateFileW(
                pipePath.c_str(),
                GENERIC_READ | GENERIC_WRITE,
                0, nullptr,
                OPEN_EXISTING,
                FILE_ATTRIBUTE_NORMAL | FILE_FLAG_OVERLAPPED,
                nullptr);

            if (rawHandle != INVALID_HANDLE_VALUE)
                return wil::unique_hfile{ rawHandle };

            const auto remainingMs = std::chrono::duration_cast<std::chrono::milliseconds>(deadline - std::chrono::steady_clock::now()).count();
            THROW_WIN32_IF(ERROR_TIMEOUT, remainingMs <= 0);

            if (!::WaitNamedPipeW(pipePath.c_str(), static_cast<DWORD>(remainingMs)))
            {
                const DWORD waitError = ::GetLastError();

                if (waitError == ERROR_FILE_NOT_FOUND ||
                    waitError == ERROR_SEM_TIMEOUT ||
                    waitError == ERROR_PIPE_BUSY)
                {
                    continue;
                }

                THROW_WIN32(waitError);
            }
        }
    }

    wil::task<std::vector<std::byte>> ReadExactly(HANDLE pipe, size_t count)
    {
        ValidatePipeHandle(pipe);

        std::vector<std::byte> bytesOwner(count);
        std::span<std::byte> bytes{ bytesOwner.data(), bytesOwner.size() };

        size_t offset = 0;
        while (offset < bytes.size())
        {
            auto chunk = bytes.subspan(offset);

            const auto read = co_await AwaitOverlappedAsync(
                pipe,
                [&](OVERLAPPED& overlapped) noexcept
            {
                return ::ReadFile(
                    pipe,
                    chunk.data(),
                    static_cast<DWORD>(chunk.size()),
                    nullptr,
                    &overlapped);
            });

            if (read == 0)
                throw std::runtime_error("Named pipe was closed while reading.");

            offset += read;
        }

        co_return bytesOwner;
    }

    wil::task<void> WriteExactly(HANDLE pipe, std::span<const std::byte> bytes)
    {
        ValidatePipeHandle(pipe);

        size_t offset = 0;
        while (offset < bytes.size())
        {
            const size_t chunkSize = bytes.size() - offset;
            auto chunk = bytes.subspan(offset, chunkSize);

            const auto written = co_await AwaitOverlappedAsync(
                pipe,
                [&](OVERLAPPED& overlapped) noexcept
            {
                return ::WriteFile(
                    pipe,
                    chunk.data(),
                    static_cast<DWORD>(chunk.size()),
                    nullptr,
                    &overlapped);
            });

            if (written == 0)
                throw std::runtime_error("Named pipe was closed while writing.");

            offset += written;
        }

        co_return;
    }
}

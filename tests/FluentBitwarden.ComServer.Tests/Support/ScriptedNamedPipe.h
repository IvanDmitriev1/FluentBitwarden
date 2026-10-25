#pragma once

#include <windows.h>
#include <wil/resource.h>

#include <algorithm>
#include <atomic>
#include <exception>
#include <span>
#include <stdexcept>
#include <string>
#include <system_error>
#include <thread>
#include <vector>

namespace FluentBitwarden::ComServer::Tests::ScriptedPipe
{
    inline constexpr DWORD WatchdogMilliseconds = 5000;

    inline std::wstring NewPipePath()
    {
        static std::atomic_uint64_t nextId{};
        return L"\\\\.\\pipe\\FluentBitwarden.ComServer.Tests." +
            std::to_wstring(::GetCurrentProcessId()) + L"." +
            std::to_wstring(nextId.fetch_add(1, std::memory_order_relaxed));
    }

    inline void ThrowLastError(const char* operation)
    {
        throw std::system_error(
            static_cast<int>(::GetLastError()),
            std::system_category(),
            operation);
    }

    template<typename StartOperation>
    DWORD CompleteOverlapped(HANDLE pipe, StartOperation&& startOperation, const char* operation)
    {
        wil::unique_handle event{ ::CreateEventW(nullptr, TRUE, FALSE, nullptr) };
        if (!event)
            ThrowLastError("CreateEventW");

        OVERLAPPED overlapped{};
        overlapped.hEvent = event.get();
        const BOOL completedSynchronously = startOperation(overlapped);
        if (!completedSynchronously)
        {
            const DWORD error = ::GetLastError();
            if (error != ERROR_IO_PENDING)
            {
                throw std::system_error(static_cast<int>(error), std::system_category(), operation);
            }

            const DWORD wait = ::WaitForSingleObject(event.get(), WatchdogMilliseconds);
            if (wait != WAIT_OBJECT_0)
            {
                const DWORD waitError = wait == WAIT_FAILED ? ::GetLastError() : ERROR_TIMEOUT;
                ::CancelIoEx(pipe, &overlapped);
                (void)::WaitForSingleObject(event.get(), INFINITE);
                DWORD ignored{};
                (void)::GetOverlappedResult(pipe, &overlapped, &ignored, FALSE);
                throw std::system_error(static_cast<int>(waitError), std::system_category(), operation);
            }
        }

        DWORD transferred{};
        if (!::GetOverlappedResult(pipe, &overlapped, &transferred, FALSE))
            ThrowLastError(operation);
        return transferred;
    }

    inline void Connect(HANDLE pipe)
    {
        wil::unique_handle event{ ::CreateEventW(nullptr, TRUE, FALSE, nullptr) };
        if (!event)
            ThrowLastError("CreateEventW");

        OVERLAPPED overlapped{};
        overlapped.hEvent = event.get();
        if (!::ConnectNamedPipe(pipe, &overlapped))
        {
            const DWORD error = ::GetLastError();
            if (error == ERROR_PIPE_CONNECTED)
                return;
            if (error != ERROR_IO_PENDING)
                throw std::system_error(static_cast<int>(error), std::system_category(), "ConnectNamedPipe");

            const DWORD wait = ::WaitForSingleObject(event.get(), WatchdogMilliseconds);
            if (wait != WAIT_OBJECT_0)
            {
                const DWORD waitError = wait == WAIT_FAILED ? ::GetLastError() : ERROR_TIMEOUT;
                ::CancelIoEx(pipe, &overlapped);
                (void)::WaitForSingleObject(event.get(), INFINITE);
                DWORD ignored{};
                (void)::GetOverlappedResult(pipe, &overlapped, &ignored, FALSE);
                throw std::system_error(static_cast<int>(waitError), std::system_category(), "ConnectNamedPipe");
            }
        }

        DWORD ignored{};
        if (!::GetOverlappedResult(pipe, &overlapped, &ignored, FALSE))
            ThrowLastError("ConnectNamedPipe");
    }

    inline DWORD ReadSome(HANDLE pipe, std::span<std::byte> buffer)
    {
        return CompleteOverlapped(pipe, [&](OVERLAPPED& overlapped)
        {
            return ::ReadFile(pipe, buffer.data(), static_cast<DWORD>(buffer.size()), nullptr, &overlapped);
        }, "ReadFile");
    }

    inline DWORD WriteSome(HANDLE pipe, std::span<const std::byte> buffer)
    {
        return CompleteOverlapped(pipe, [&](OVERLAPPED& overlapped)
        {
            return ::WriteFile(
                pipe,
                const_cast<std::byte*>(buffer.data()),
                static_cast<DWORD>(buffer.size()),
                nullptr,
                &overlapped);
        }, "WriteFile");
    }

    inline std::vector<std::byte> ReadExactly(HANDLE pipe, std::size_t count)
    {
        std::vector<std::byte> bytes(count);
        std::size_t offset{};
        while (offset < bytes.size())
        {
            const DWORD read = ReadSome(pipe, std::span<std::byte>{ bytes }.subspan(offset));
            if (read == 0)
                throw std::runtime_error("Named pipe closed before the expected bytes arrived.");
            offset += read;
        }
        return bytes;
    }

    inline void WriteExactly(HANDLE pipe, std::span<const std::byte> bytes)
    {
        std::size_t offset{};
        while (offset < bytes.size())
        {
            const DWORD written = WriteSome(pipe, bytes.subspan(offset));
            if (written == 0)
                throw std::runtime_error("Named pipe closed before the expected bytes were written.");
            offset += written;
        }
    }

    inline void WriteFragments(HANDLE pipe, std::span<const std::byte> bytes, std::span<const std::size_t> fragmentSizes)
    {
        std::size_t offset{};
        for (const std::size_t fragmentSize : fragmentSizes)
        {
            if (fragmentSize == 0 || fragmentSize > bytes.size() - offset)
                throw std::invalid_argument("Fragment sizes must cover non-empty parts of the input.");
            WriteExactly(pipe, bytes.subspan(offset, fragmentSize));
            offset += fragmentSize;
        }
        if (offset != bytes.size())
            throw std::invalid_argument("Fragment sizes must cover the complete input.");
    }

    inline void WaitForRelease(HANDLE releaseEvent)
    {
        const DWORD wait = ::WaitForSingleObject(releaseEvent, WatchdogMilliseconds);
        if (wait == WAIT_TIMEOUT)
            throw std::system_error(ERROR_TIMEOUT, std::system_category(), "scripted pipe watchdog");
        if (wait == WAIT_FAILED)
            ThrowLastError("WaitForSingleObject");
    }

    class Server final
    {
    public:
        template<typename Script>
        explicit Server(Script script)
            : m_path(NewPipePath()),
              m_pipe(::CreateNamedPipeW(
                  m_path.c_str(),
                  PIPE_ACCESS_DUPLEX | FILE_FLAG_OVERLAPPED,
                  PIPE_TYPE_BYTE | PIPE_READMODE_BYTE | PIPE_WAIT,
                  1,
                  64 * 1024,
                  64 * 1024,
                  0,
                  nullptr)),
              m_release(::CreateEventW(nullptr, TRUE, FALSE, nullptr))
        {
            if (!m_pipe || m_pipe.get() == INVALID_HANDLE_VALUE)
                ThrowLastError("CreateNamedPipeW");
            if (!m_release)
                ThrowLastError("CreateEventW");

            m_worker = std::thread([this, script = std::move(script)]() mutable
            {
                bool connected{};
                try
                {
                    if (::WaitForSingleObject(m_release.get(), 0) != WAIT_OBJECT_0)
                    {
                        Connect(m_pipe.get());
                        connected = true;
                        script(m_pipe.get(), m_release.get());
                    }
                }
                catch (...)
                {
                    m_error = std::current_exception();
                }

                if (connected)
                    (void)::DisconnectNamedPipe(m_pipe.get());
            });
        }

        ~Server()
        {
            Release();
            if (m_pipe && m_pipe.get() != INVALID_HANDLE_VALUE)
                (void)::CancelIoEx(m_pipe.get(), nullptr);
            if (m_worker.joinable())
                m_worker.join();
        }

        Server(const Server&) = delete;
        Server& operator=(const Server&) = delete;

        [[nodiscard]] const std::wstring& Path() const noexcept { return m_path; }

        void ReleaseAndJoin()
        {
            Release();
            if (m_worker.joinable())
                m_worker.join();
            if (m_error)
                std::rethrow_exception(m_error);
        }

        void Release() noexcept
        {
            if (m_release)
                (void)::SetEvent(m_release.get());
        }

    private:
        std::wstring m_path;
        wil::unique_hfile m_pipe;
        wil::unique_handle m_release;
        std::thread m_worker;
        std::exception_ptr m_error;
    };
}

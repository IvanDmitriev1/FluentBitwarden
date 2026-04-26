#pragma once
#include <pch.h>

namespace FluentBitwarden::ComServer::Ipc::PipeWin32
{
    [[nodiscard]] wil::unique_hfile OpenOverlappedPipe(const std::wstring& pipePath, TimeSpan timeout);

    IAsyncOperation<IBuffer> ReadExactly(HANDLE pipe, size_t count);
    IAsyncAction WriteExactly(HANDLE pipe, std::span<const std::byte> bytes);
}
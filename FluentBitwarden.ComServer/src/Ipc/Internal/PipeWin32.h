#pragma once
#include <pch.h>

namespace FluentBitwarden::ComServer::Ipc::PipeWin32
{
    [[nodiscard]] wil::unique_hfile OpenOverlappedPipe(const std::wstring& pipePath, TimeSpan timeout);

    wil::task<std::vector<std::byte>> ReadExactly(HANDLE pipe, size_t count);
    wil::task<void> WriteExactly(HANDLE pipe, std::span<const std::byte> bytes);
}

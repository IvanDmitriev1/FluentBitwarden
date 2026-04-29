#pragma once
#include <pch.h>

namespace FluentBitwarden::ComServer::Utils
{
	[[nodiscard]] winrt::hstring Base64UrlEncode(std::span<const std::uint8_t> bytes);
	[[nodiscard]] std::vector<std::uint8_t> Base64UrlDecode(const winrt::hstring& encoded);
}

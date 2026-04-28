#pragma once
#include <pch.h>

namespace FluentBitwarden::ComServer::Utils
{
	HRESULT ComputeSha256(std::span<const std::uint8_t> data, std::vector<std::uint8_t>& hash) noexcept;
}
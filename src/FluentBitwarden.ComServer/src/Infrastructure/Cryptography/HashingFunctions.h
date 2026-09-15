#pragma once
#include <pch.h>

namespace FluentBitwarden::ComServer::Utils
{
	std::vector<std::uint8_t> ComputeSha256(std::span<const std::uint8_t> data);
}

#pragma once
#include <windows.h>
#include <unknwn.h>
#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Foundation.Collections.h>

std::vector<uint8_t> hexStringToBytes(const std::string& hex);
#pragma once

#include <wil/cppwinrt.h>
#include <wil/resource.h>
#include <wil/result.h>
#include <wil/result_macros.h>
#include <wil/registry.h>
#include <wil/win32_helpers.h>
#include <wil/coroutine.h>

//Classic COM FIRST
#include <windows.h>
#include <unknwn.h>

// Re-include after Windows headers so wil::task::get() is defined.
#include <wil/coroutine.h>

#include <webauthn.h>
#include <pluginauthenticator.h>
#include <webauthnplugin.h>

#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Foundation.Collections.h>
#include <winrt/Windows.System.h>
#include <winrt/Windows.Data.Json.h>
#include <winrt/Windows.ApplicationModel.h>
#include <winrt/Windows.Storage.h>

#include <array>
#include <filesystem>
#include <cstdint>
#include <string>
#include <string_view>
#include <concepts>
#include <chrono>
#include <cstddef>
#include <cstdint>
#include <bit>
#include <span>
#include <stdexcept>
#include <vector>

using winrt::Windows::Data::Json::JsonObject;
using winrt::Windows::Data::Json::JsonValue;
using winrt::Windows::Foundation::TimeSpan;

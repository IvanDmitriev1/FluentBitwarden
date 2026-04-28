#pragma once

#include <wil/cppwinrt.h>
#include <wil/resource.h>
#include <wil/result.h>
#include <wil/result_macros.h>
#include <wil/registry.h>
#include <wil/win32_helpers.h>

//Classic COM FIRST
#include <windows.h>
#include <unknwn.h>

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
#include <winrt/Windows.Security.Cryptography.h>
#include <winrt/Windows.Storage.Streams.h>

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
#include <ranges>

using winrt::Windows::Data::Json::JsonObject;
using winrt::Windows::Data::Json::JsonValue;
using winrt::Windows::Foundation::TimeSpan;

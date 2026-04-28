#pragma once
#include <optional>
#include "IpcProtocol.h"

namespace FluentBitwarden::ComServer::Ipc
{
	template<IpcJsonResponse T>
	struct IpcResult
	{
		bool Success{};
		std::optional<T> Value;
		std::optional<winrt::hstring> Error;

		[[nodiscard]] static IpcResult<T> FromJson(JsonObject const& json)
		{
			THROW_HR_IF_MSG(E_INVALIDARG, !json.HasKey(L"Success"), "Missing IPC result Success field.");

			const bool success = json.GetNamedBoolean(L"Success");
			if (success)
			{
				THROW_HR_IF_MSG(E_INVALIDARG, !json.HasKey(L"Value"), "Missing IPC result Value field.");

				return IpcResult<T>
				{
					true,
					T::FromJson(json.GetNamedObject(L"Value")),
					std::nullopt
				};
			}

			THROW_HR_IF_MSG(E_INVALIDARG, !json.HasKey(L"Error"), "Missing IPC result Error field.");

			return IpcResult<T>
			{
				false,
				std::nullopt,
				json.GetNamedString(L"Error")
			};
		}

		[[nodiscard]] T& ValueOrThrow()
		{
			if (Success)
			{
				if (!Value)
				{
					THROW_HR_MSG(E_UNEXPECTED, "IPC success result has no value.");
				}

				return *Value;
			}

			const winrt::hstring message = Error ? *Error : winrt::hstring{ L"IPC request failed." };
			THROW_HR_MSG(E_FAIL, "%ls", message.c_str());
		}

		[[nodiscard]] const T& ValueOrThrow() const
		{
			if (Success)
			{
				if (!Value)
				{
					THROW_HR_MSG(E_UNEXPECTED, "IPC success result has no value.");
				}

				return *Value;
			}

			const winrt::hstring message = Error ? *Error : winrt::hstring{ L"IPC request failed." };
			THROW_HR_MSG(E_FAIL, "%ls", message.c_str());
		}
	};
}

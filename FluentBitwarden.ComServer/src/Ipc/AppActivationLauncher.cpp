#include "pch.h"
#include "AppActivationLauncher.h"

#include <array>
#include <filesystem>
#include <winrt/Windows.ApplicationModel.h>
#include <winrt/Windows.Storage.h>

HRESULT AppActivationLauncher::ActivateMainApp(const std::wstring& launchArguments) noexcept
{
	try
	{
		std::filesystem::path packageRoot{ winrt::Windows::ApplicationModel::Package::Current().InstalledLocation().Path().c_str() };
		std::filesystem::path mainAppPath{ packageRoot / "FluentBitwarden" / "FluentBitwarden.exe" };

		std::wstring commandLine = L"\"" + mainAppPath.native() + L"\"";
		if (!launchArguments.empty())
		{
			commandLine += L" ";
			commandLine += launchArguments;
		}

		STARTUPINFOW startupInfo{};
		startupInfo.cb = sizeof(startupInfo);

		PROCESS_INFORMATION processInfo{};
		RETURN_LAST_ERROR_IF(!CreateProcessW(
			mainAppPath.c_str(),
			commandLine.data(),
			nullptr,
			nullptr,
			FALSE,
			0,
			nullptr,
			mainAppPath.parent_path().c_str(),
			&startupInfo,
			&processInfo));

		CloseHandle(processInfo.hThread);
		CloseHandle(processInfo.hProcess);
		return S_OK;
	}
	CATCH_RETURN();
}

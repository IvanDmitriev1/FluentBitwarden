#include "pch.h"
#include "AppActivationLauncher.h"

#include <winrt/Windows.ApplicationModel.h>

HRESULT AppActivationLauncher::ActivateMainApp(const std::wstring& launchArguments) noexcept
{
	try
	{
		winrt::Windows::ApplicationModel::FullTrustProcessLauncher::LaunchFullTrustProcessForCurrentAppWithArgumentsAsync(launchArguments).get();
		return S_OK;
	}
	CATCH_RETURN();
}

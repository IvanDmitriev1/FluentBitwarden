#include "pch.h"
#include "Passkey/PluginAuthenticator.h"

#include "Infrastructure/AppHostLauncher/AppHostLauncher.h"
#include "Infrastructure/Ipc/IpcProtocol.h"
#include "Passkey/Ipc/AppHostPasskeyClient.h"

namespace FluentBitwarden::ComServer
{
	IFACEMETHODIMP PluginAuthenticatorFactory::CreateInstance(IUnknown* outer, REFIID iid, void** result) noexcept
	{
		RETURN_HR_IF(CLASS_E_NOAGGREGATION, outer != nullptr);
		RETURN_HR_IF_NULL(E_POINTER, result);
		*result = nullptr;

		try
		{
			auto passkeyClient = std::make_unique<AppHostPasskeyClient>(std::wstring{ Ipc::Constants::PipePath });
			auto appHostLauncher = std::make_unique<AppHostLauncher>();

			auto obj = winrt::make<PluginAuthenticator>(
				std::move(passkeyClient),
				std::move(appHostLauncher));

			RETURN_IF_FAILED(obj->QueryInterface(iid, result));
			return S_OK;
		}
		CATCH_RETURN();
	}
}

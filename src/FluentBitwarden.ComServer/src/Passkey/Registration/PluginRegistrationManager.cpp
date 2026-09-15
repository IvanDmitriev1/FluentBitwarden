#include "pch.h"
#include "Passkey/PluginAuthenticator.h"
#include "Passkey/Registration/PluginRegistrationManager.h"
#include "Passkey/Registration/AuthenticatorInfoBuilder.h"

namespace FluentBitwarden::ComServer
{

	using unique_add_auth_response =
		wil::unique_any<
		PWEBAUTHN_PLUGIN_ADD_AUTHENTICATOR_RESPONSE,
		decltype(&WebAuthNPluginFreeAddAuthenticatorResponse),
		WebAuthNPluginFreeAddAuthenticatorResponse>;

	using unique_public_key =
		wil::unique_any<
		PBYTE,
		decltype(&WebAuthNPluginFreePublicKeyResponse),
		WebAuthNPluginFreePublicKeyResponse>;

	void PluginRegistrationManager::EnsureRegistered()
	{
		if (!IsRegistered())
		{
			RegisterNew();
			return;
		}

		UpdateExisting();
	}

	void PluginRegistrationManager::Unregister()
	{
		THROW_IF_FAILED(WebAuthNPluginRemoveAuthenticator(PluginAuthenticator::CLSID));
	}

	std::vector<std::uint8_t> PluginRegistrationManager::GetOperationSigningPublicKey()
	{
		DWORD cbKey = 0;
		PBYTE rawKey = nullptr;

		THROW_IF_FAILED(
			WebAuthNPluginGetOperationSigningPublicKey(
			PluginAuthenticator::CLSID,
			&cbKey,
			&rawKey));

		unique_public_key key{ rawKey };
		THROW_HR_IF(E_UNEXPECTED, cbKey == 0 || key.get() == nullptr);

		std::vector<std::uint8_t> publicKey(cbKey);
		std::copy_n(key.get(), cbKey, publicKey.begin());
		return publicKey;
	}

	void PluginRegistrationManager::RegisterNew()
	{
		auto authenticatorInfo = PasskeyPlugin::Registration::BuildAuthenticatorGetInfoCbor();

		WEBAUTHN_PLUGIN_ADD_AUTHENTICATOR_OPTIONS options
		{
			.pwszAuthenticatorName = kPluginName.data(),
			.rclsid = PluginAuthenticator::CLSID,
			.pwszPluginRpId = kPluginRpId.data(),
			.pwszLightThemeLogoSvg = nullptr,
			.pwszDarkThemeLogoSvg = nullptr,
			.cbAuthenticatorInfo = static_cast<DWORD>(authenticatorInfo.size()),
			.pbAuthenticatorInfo = authenticatorInfo.data(),
			.cSupportedRpIds = 0,
			.ppwszSupportedRpIds = nullptr,
		};

		PWEBAUTHN_PLUGIN_ADD_AUTHENTICATOR_RESPONSE rawResponse = nullptr;
		THROW_IF_FAILED(WebAuthNPluginAddAuthenticator(&options, &rawResponse));
		unique_add_auth_response response{ rawResponse };
	}

	void PluginRegistrationManager::UpdateExisting()
	{
		auto authenticatorInfo = PasskeyPlugin::Registration::BuildAuthenticatorGetInfoCbor();

		WEBAUTHN_PLUGIN_UPDATE_AUTHENTICATOR_DETAILS details
		{
			.pwszAuthenticatorName = kPluginName.data(),
			.rclsid = PluginAuthenticator::CLSID,
			.rclsidNew = PluginAuthenticator::CLSID,
			.pwszLightThemeLogoSvg = nullptr,
			.pwszDarkThemeLogoSvg = nullptr,
			.cbAuthenticatorInfo = static_cast<DWORD>(authenticatorInfo.size()),
			.pbAuthenticatorInfo = authenticatorInfo.data(),
			.cSupportedRpIds = 0,
			.ppwszSupportedRpIds = nullptr,
		};

		THROW_IF_FAILED(WebAuthNPluginUpdateAuthenticatorDetails(&details));
	}

	bool PluginRegistrationManager::IsRegistered()
	{
		AUTHENTICATOR_STATE state{};
		const HRESULT hr = WebAuthNPluginGetAuthenticatorState(
			PluginAuthenticator::CLSID,
			&state);

		return SUCCEEDED(hr);
	}

}

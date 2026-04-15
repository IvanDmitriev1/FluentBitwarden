#include "pch.h"
#include "PluginRegistrationManager.h"
#include "PluginAuthenticator.h"
#include "AuthenticatorInfoBuilder.h"

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

HRESULT PluginRegistrationManager::EnsureRegistered() noexcept
{
	bool registered = false;
	RETURN_IF_FAILED(IsRegistered(registered));

	if (!registered)
	{
		RETURN_IF_FAILED(RegisterNew());
		return S_OK;
	}

	RETURN_IF_FAILED(UpdateExisting());
	return S_OK;
}

HRESULT PluginRegistrationManager::Unregister() noexcept
{
	return WebAuthNPluginRemoveAuthenticator(PluginAuthenticator::CLSID);
}

HRESULT PluginRegistrationManager::GetOperationSigningPublicKey(std::vector<std::uint8_t>& publicKey) noexcept
{
	publicKey.clear();

	DWORD cbKey = 0;
	PBYTE rawKey = nullptr;

	RETURN_IF_FAILED(
		WebAuthNPluginGetOperationSigningPublicKey(
		PluginAuthenticator::CLSID,
		&cbKey,
		&rawKey));

	unique_public_key key{ rawKey };
	RETURN_HR_IF(E_UNEXPECTED, cbKey == 0 || key.get() == nullptr);

	publicKey.resize(cbKey);
	std::copy_n(key.get(), cbKey, publicKey.begin());
	return S_OK;
}

HRESULT PluginRegistrationManager::RegisterNew() noexcept
{
	auto authenticatorInfo = FluentBitwarden::PasskeyPlugin::Registration::BuildAuthenticatorGetInfoCbor();

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
	RETURN_IF_FAILED(WebAuthNPluginAddAuthenticator(&options, &rawResponse));
	unique_add_auth_response response{ rawResponse };

	return S_OK;
}

HRESULT PluginRegistrationManager::UpdateExisting() noexcept
{
	auto authenticatorInfo = FluentBitwarden::PasskeyPlugin::Registration::BuildAuthenticatorGetInfoCbor();

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

	return WebAuthNPluginUpdateAuthenticatorDetails(&details);
}

HRESULT PluginRegistrationManager::IsRegistered(bool& registered) noexcept
{
	registered = false;

	AUTHENTICATOR_STATE state{};
	const HRESULT hr = WebAuthNPluginGetAuthenticatorState(
		PluginAuthenticator::CLSID,
		&state);

	if (SUCCEEDED(hr))
	{
		registered = true;
		return S_OK;
	}

	registered = false;
	return S_OK;
}


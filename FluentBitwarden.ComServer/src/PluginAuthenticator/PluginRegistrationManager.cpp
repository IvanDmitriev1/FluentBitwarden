#include "pch.h"
#include "PluginRegistrationManager.h"
#include "PluginAuthenticator.h"
#include <webauthn.h>
#include <webauthnplugin.h>
#include <pluginauthenticator.h>

PluginRegistrationManager::PluginRegistrationManager()
	:m_initialized(false),
	m_pluginRegistered(false)
{
	Initialize();
}

HRESULT PluginRegistrationManager::Initialize()
{
	return E_NOTIMPL;
}

HRESULT PluginRegistrationManager::RegisterPlugin()
{
    std::string tempAaguidStr{ kPluginAaguid };
    tempAaguidStr.erase(std::remove(tempAaguidStr.begin(), tempAaguidStr.end(), L'-'), tempAaguidStr.end());
    std::transform(tempAaguidStr.begin(), tempAaguidStr.end(), tempAaguidStr.begin(), [](unsigned char c) { return static_cast<char>(std::toupper(c)); });
    // The following hex strings represent the encoding of
    // {1: ["FIDO_2_0", "FIDO_2_1"], 2: ["prf", "hmac-secret"], 3: h'/* AAGUID */', 4: {"rk": true, "up": true, "uv": true}, 
    // 9: ["internal"], 10: [{"alg": -7, "type": "public-key"}]}
    std::string authenticatorInfoStrPart1 = "A60182684649444F5F325F30684649444F5F325F310282637072666B686D61632D7365637265740350";
    std::string authenticatorInfoStrPart2 = "04A362726BF5627570F5627576F5098168696E7465726E616C0A81A263616C672664747970656A7075626C69632D6B6579";
    std::string fullAuthenticatorInfoStr = authenticatorInfoStrPart1 + tempAaguidStr + authenticatorInfoStrPart2;
    std::vector<BYTE> authenticatorInfo = hexStringToBytes(fullAuthenticatorInfoStr);


    WEBAUTHN_PLUGIN_ADD_AUTHENTICATOR_OPTIONS opts
    {
        .pwszAuthenticatorName = kPluginName,
        .rclsid = PluginAuthenticatorImpl::CLSID,
        .pwszPluginRpId = nullptr,
        .pwszLightThemeLogoSvg = nullptr,
        .pwszDarkThemeLogoSvg = nullptr,
        .cbAuthenticatorInfo = static_cast<DWORD>(authenticatorInfo.size()),
        .pbAuthenticatorInfo = authenticatorInfo.data(),
        .cSupportedRpIds = 0,
        .ppwszSupportedRpIds = nullptr,
    };

    PWEBAUTHN_PLUGIN_ADD_AUTHENTICATOR_RESPONSE response = nullptr;
    HRESULT hr = WebAuthNPluginAddAuthenticator(&opts, &response);

    if (FAILED(hr))
        return hr;

    WebAuthNPluginFreeAddAuthenticatorResponse(response);
    return S_OK;
}

HRESULT PluginRegistrationManager::UnregisterPlugin()
{
	return E_NOTIMPL;
}

HRESULT PluginRegistrationManager::UpdatePlugin()
{
	return E_NOTIMPL;
}

HRESULT PluginRegistrationManager::RefreshPluginState()
{
	return E_NOTIMPL;
}


HRESULT PluginRegistrationManager::SaveOperationSigningKey(const BYTE* pbOpSignPubKey, DWORD cbOpSignPubKey)
{
    HKEY hKey = nullptr;
    LONG rc = RegCreateKeyExW(
        HKEY_CURRENT_USER,
        kRegistryPath,
        0,
        nullptr,
        REG_OPTION_NON_VOLATILE,
        KEY_WRITE,
        nullptr,
        &hKey,
        nullptr);

    if (rc != ERROR_SUCCESS)
        return HRESULT_FROM_WIN32(rc);

    rc = RegSetValueExW(
        hKey,
        kRequestSigningKeyValue,
        0,
        REG_BINARY,
        pbOpSignPubKey,
        cbOpSignPubKey);

    RegCloseKey(hKey);

    if (rc != ERROR_SUCCESS)
        return HRESULT_FROM_WIN32(rc);

    return S_OK;
}

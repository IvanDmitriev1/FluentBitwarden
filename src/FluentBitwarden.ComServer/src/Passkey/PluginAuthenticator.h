#pragma once
#include "pch.h"
#include "Infrastructure/AppHostLauncher/IAppHostLauncher.h"
#include "Passkey/Ipc/IPasskeyClient.h"

#include <memory>

namespace FluentBitwarden::ComServer
{
    struct PluginAuthenticatorFactory : winrt::implements<PluginAuthenticatorFactory, IClassFactory>
    {
        IFACEMETHODIMP CreateInstance(IUnknown* outer, REFIID iid, void** result) noexcept override;
        IFACEMETHODIMP LockServer(BOOL) noexcept override { return S_OK; }
    };

    struct PluginAuthenticator : winrt::implements<PluginAuthenticator, IPluginAuthenticator>
    {
        static constexpr GUID CLSID =
        { 0x3c4f8d12, 0x7a6b, 0x4e91, { 0x9a, 0x37, 0xc2, 0x5d, 0x14, 0x8f, 0xb6, 0x70 } };

        PluginAuthenticator(
            std::unique_ptr<IPasskeyClient> passkeyClient,
            std::unique_ptr<IAppHostLauncher> appHostLauncher);

        IFACEMETHODIMP MakeCredential(
            PCWEBAUTHN_PLUGIN_OPERATION_REQUEST request,
            PWEBAUTHN_PLUGIN_OPERATION_RESPONSE response) noexcept override;

        IFACEMETHODIMP GetAssertion(
            PCWEBAUTHN_PLUGIN_OPERATION_REQUEST request,
            PWEBAUTHN_PLUGIN_OPERATION_RESPONSE response) noexcept override;

        IFACEMETHODIMP CancelOperation(
            PCWEBAUTHN_PLUGIN_CANCEL_OPERATION_REQUEST request) noexcept override;

        IFACEMETHODIMP GetLockStatus(
            PLUGIN_LOCK_STATUS* lockStatus) noexcept override;

    private:
        std::unique_ptr<IPasskeyClient> m_passkeyClient;
        std::unique_ptr<IAppHostLauncher> m_appHostLauncher;
    };
}

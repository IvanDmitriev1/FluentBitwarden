#pragma once
#include "pch.h"
#include <pluginauthenticator.h>

struct PluginAuthenticatorImpl : winrt::implements<PluginAuthenticatorImpl, IPluginAuthenticator>
{
    static constexpr GUID CLSID =
    { 0x6fa0e3e9, 0xb255, 0x48cf, { 0x8e, 0x2c, 0x7d, 0x8b, 0x6e, 0x4a, 0x91, 0xf3 } };

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
};


struct PluginAuthenticatorFactory : winrt::implements<PluginAuthenticatorFactory, IClassFactory>
{
    IFACEMETHODIMP CreateInstance(IUnknown* outer, REFIID iid, void** result) noexcept override;
    IFACEMETHODIMP LockServer(BOOL) noexcept override { return S_OK; }
};
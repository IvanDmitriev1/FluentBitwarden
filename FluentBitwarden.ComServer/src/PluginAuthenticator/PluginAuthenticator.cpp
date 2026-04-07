#include "pch.h"
#include "PluginAuthenticator.h"

IFACEMETHODIMP PluginAuthenticatorFactory::CreateInstance(IUnknown* outer, REFIID iid, void** result) noexcept
{
    if (outer)
        return CLASS_E_NOAGGREGATION;

    auto obj = winrt::make_self<PluginAuthenticatorImpl>();
    return obj->QueryInterface(iid, result);
}


IFACEMETHODIMP PluginAuthenticatorImpl::MakeCredential(PCWEBAUTHN_PLUGIN_OPERATION_REQUEST request, PWEBAUTHN_PLUGIN_OPERATION_RESPONSE response) noexcept
{
    if (!request || !response)
        return E_INVALIDARG;

    *response = {};

    MessageBoxW(nullptr,
                L"Demo passkey provider was activated for MakeCredential.",
                L"Demo Passkey Manager",
                MB_OK | MB_ICONINFORMATION);

    return NTE_NOT_SUPPORTED;
}

IFACEMETHODIMP PluginAuthenticatorImpl::GetAssertion(PCWEBAUTHN_PLUGIN_OPERATION_REQUEST request, PWEBAUTHN_PLUGIN_OPERATION_RESPONSE response) noexcept
{
    if (!request || !response)
        return E_INVALIDARG;

    *response = {};

    MessageBoxW(nullptr,
                L"Demo passkey provider was activated for GetAssertion.",
                L"Demo Passkey Manager",
                MB_OK | MB_ICONINFORMATION);

    return NTE_NOT_SUPPORTED;
}

IFACEMETHODIMP PluginAuthenticatorImpl::CancelOperation(PCWEBAUTHN_PLUGIN_CANCEL_OPERATION_REQUEST request) noexcept
{
    if (!request)
        return E_INVALIDARG;

    return S_OK;

}

IFACEMETHODIMP PluginAuthenticatorImpl::GetLockStatus(PLUGIN_LOCK_STATUS* lockStatus) noexcept
{
    if (!lockStatus)
        return E_POINTER;

    // Minimal demo: always unlocked.
    *lockStatus = PluginUnlocked;
    return S_OK;
}

#include "pch.h"
#include "PluginAuthenticator.h"
#include "WebAuthn/OperationRequestVerifier.h"
#include "Ipc/AppActivationLauncher.h"

IFACEMETHODIMP PluginAuthenticatorFactory::CreateInstance(IUnknown* outer, REFIID iid, void** result) noexcept
{
	if (outer)
		return CLASS_E_NOAGGREGATION;

	auto obj = winrt::make<PluginAuthenticator>();
	return obj->QueryInterface(iid, result);
}

IFACEMETHODIMP PluginAuthenticator::MakeCredential(PCWEBAUTHN_PLUGIN_OPERATION_REQUEST request, PWEBAUTHN_PLUGIN_OPERATION_RESPONSE response) noexcept
{
	if (!request || !response)
		return E_POINTER;

	RETURN_IF_FAILED(OperationRequestVerifier::VerifyOperationRequest(*request));

	AppActivationLauncher::ActivateMainApp(L"--passkey");

	return E_NOTIMPL;
}

IFACEMETHODIMP PluginAuthenticator::GetAssertion(PCWEBAUTHN_PLUGIN_OPERATION_REQUEST request, PWEBAUTHN_PLUGIN_OPERATION_RESPONSE response) noexcept
{
	if (!request || !response)
		return E_POINTER;

	RETURN_IF_FAILED(OperationRequestVerifier::VerifyOperationRequest(*request));

	return E_NOTIMPL;
}

IFACEMETHODIMP PluginAuthenticator::CancelOperation(PCWEBAUTHN_PLUGIN_CANCEL_OPERATION_REQUEST request) noexcept
{
	if (!request)
		return E_POINTER;


	return S_OK;
}

IFACEMETHODIMP PluginAuthenticator::GetLockStatus(PLUGIN_LOCK_STATUS* lockStatus) noexcept
{
	if (!lockStatus)
		return E_POINTER;

	*lockStatus = PluginUnlocked;
	return S_OK;
}

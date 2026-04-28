#include "pch.h"
#include "PluginAuthenticator.h"
#include "Ipc/AppActivationLauncher.h"
#include "Ipc/NamedPipeClient.h"

#include "WebAuthn/OperationRequestVerifier.h"
#include "WebAuthn/DecodedWebAuthnGetAssertionRequest.h"
#include "WebAuthn/AssertionResponseBuilder.h"

using namespace FluentBitwarden::ComServer::WebAuthn;
using namespace FluentBitwarden::ComServer;

IFACEMETHODIMP PluginAuthenticatorFactory::CreateInstance(IUnknown* outer, REFIID iid, void** result) noexcept
{
	if (outer)
		return CLASS_E_NOAGGREGATION;

	auto obj = winrt::make<PluginAuthenticator>();
	return obj->QueryInterface(iid, result);
}

IFACEMETHODIMP PluginAuthenticator::MakeCredential(PCWEBAUTHN_PLUGIN_OPERATION_REQUEST request, PWEBAUTHN_PLUGIN_OPERATION_RESPONSE response) noexcept
{
	RETURN_HR_IF(E_POINTER, !request || !response);
	RETURN_IF_FAILED(OperationRequestVerifier::VerifyOperationRequest(*request));

	return E_NOTIMPL;
}

IFACEMETHODIMP PluginAuthenticator::GetAssertion(PCWEBAUTHN_PLUGIN_OPERATION_REQUEST request, PWEBAUTHN_PLUGIN_OPERATION_RESPONSE response) noexcept
{
	RETURN_HR_IF(E_POINTER, !request || !response);
	RETURN_IF_FAILED(OperationRequestVerifier::VerifyOperationRequest(*request));

	if (response)
	{
		*response = {};
	}

	try
	{
		DecodedGetAssertionRequest decodedRequest;
		RETURN_IF_FAILED(DecodedGetAssertionRequest::Decode(request, decodedRequest));

		PasskeyGetAssertionRequest ipcRequest{};
		RETURN_IF_FAILED(decodedRequest.ToIpcRequest(ipcRequest));

		Ipc::AppActivationLauncher::ActivateMainApp(L"--passkey");
		Ipc::NamedPipeClient m_pipeClient{ Ipc::Constants::PipePath };

		auto ipcResult = m_pipeClient.SendAsync<PasskeyGetAssertionRequest, PasskeyAssertionResponse>(std::move(ipcRequest)).get();
		RETURN_IF_FAILED(AssertionResponseBuilder::BuildResponse(ipcResult.ValueOrThrow(), response));

		return S_OK;
	}
	CATCH_RETURN();

	return E_NOTIMPL;
}

IFACEMETHODIMP PluginAuthenticator::CancelOperation(PCWEBAUTHN_PLUGIN_CANCEL_OPERATION_REQUEST request) noexcept
{
	RETURN_HR_IF(E_POINTER, !request);


	return S_OK;
}

IFACEMETHODIMP PluginAuthenticator::GetLockStatus(PLUGIN_LOCK_STATUS* lockStatus) noexcept
{
	RETURN_HR_IF(E_POINTER, !lockStatus);

	*lockStatus = PluginLocked;
	return S_OK;
}

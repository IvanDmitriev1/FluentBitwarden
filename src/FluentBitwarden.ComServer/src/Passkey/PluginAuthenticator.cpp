#include "pch.h"
#include "Passkey/PluginAuthenticator.h"
#include "Application/Activation/AppActivationLauncher.h"
#include "Infrastructure/Ipc/AppNamedPipeClient.h"

#include "Passkey/WebAuthn/OperationRequestVerifier.h"
#include "Passkey/WebAuthn/RequestDecoder.h"
#include "Passkey/WebAuthn/ResponseEncoder.h"

namespace FluentBitwarden::ComServer
{
	IFACEMETHODIMP PluginAuthenticatorFactory::CreateInstance(IUnknown* outer, REFIID iid, void** result) noexcept
	{
		RETURN_HR_IF(CLASS_E_NOAGGREGATION, outer != nullptr);
		RETURN_HR_IF_NULL(E_POINTER, result);
		*result = nullptr;

		auto obj = winrt::make<PluginAuthenticator>();
		RETURN_IF_FAILED(obj->QueryInterface(iid, result));

		return S_OK;
	}

	IFACEMETHODIMP PluginAuthenticator::MakeCredential(PCWEBAUTHN_PLUGIN_OPERATION_REQUEST request, PWEBAUTHN_PLUGIN_OPERATION_RESPONSE response) noexcept
	{
		try
		{
			RETURN_HR_IF(E_POINTER, request == nullptr || response == nullptr);
			WebAuthn::OperationRequestVerifier::VerifyOperationRequest(*request);

            auto ipcRequest = WebAuthn::RequestDecoder::DecodeMakeCredential(request);

            Ipc::AppNamedPipeClient m_pipeClient;
            auto credential = m_pipeClient.SendAsync<WebAuthn::PasskeyMakeCredentialRequest, WebAuthn::PasskeyMakeCredentialResponse>(std::move(ipcRequest)).get();
            WebAuthn::ResponseEncoder::EncodeMakeCredential(credential, response);

			RETURN_HR(S_OK);
		}
		CATCH_RETURN();
	}

	IFACEMETHODIMP PluginAuthenticator::GetAssertion(PCWEBAUTHN_PLUGIN_OPERATION_REQUEST request, PWEBAUTHN_PLUGIN_OPERATION_RESPONSE response) noexcept
	{
		try
		{
			RETURN_HR_IF(E_POINTER, request == nullptr || response == nullptr);
			WebAuthn::OperationRequestVerifier::VerifyOperationRequest(*request);
			*response = {};

            auto ipcRequest = WebAuthn::RequestDecoder::DecodeGetAssertion(request);

            Ipc::AppNamedPipeClient m_pipeClient;
			auto assertion = m_pipeClient.SendAsync<WebAuthn::PasskeyGetAssertionRequest, WebAuthn::PasskeyAssertionResponse>(std::move(ipcRequest)).get();
			WebAuthn::ResponseEncoder::EncodeGetAssertion(assertion, response);

			return S_OK;
		}
		CATCH_RETURN();
	}

	IFACEMETHODIMP PluginAuthenticator::CancelOperation(PCWEBAUTHN_PLUGIN_CANCEL_OPERATION_REQUEST request) noexcept
	{
		RETURN_HR_IF_NULL(E_POINTER, request);

		return S_OK;
	}

	IFACEMETHODIMP PluginAuthenticator::GetLockStatus(PLUGIN_LOCK_STATUS* lockStatus) noexcept
	{
		try
		{
			RETURN_HR_IF_NULL(E_POINTER, lockStatus);

			AppActivationLauncher::ActivateAppHost(L"--headless");

			*lockStatus = PluginLocked;
			return S_OK;
		}
		CATCH_RETURN();
	}

}

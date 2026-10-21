#include "pch.h"
#include "Passkey/PluginAuthenticator.h"
#include "Passkey/WebAuthn/OperationRequestVerifier.h"
#include "Passkey/WebAuthn/RequestDecoder.h"
#include "Passkey/WebAuthn/ResponseEncoder.h"

namespace FluentBitwarden::ComServer
{
	PluginAuthenticator::PluginAuthenticator(
		std::unique_ptr<IPasskeyClient> passkeyClient,
		std::unique_ptr<IAppHostLauncher> appHostLauncher)
		: m_passkeyClient(std::move(passkeyClient)),
		  m_appHostLauncher(std::move(appHostLauncher))
	{
		
	}

	IFACEMETHODIMP PluginAuthenticator::MakeCredential(PCWEBAUTHN_PLUGIN_OPERATION_REQUEST request, PWEBAUTHN_PLUGIN_OPERATION_RESPONSE response) noexcept
	{
		try
		{
			RETURN_HR_IF(E_POINTER, request == nullptr || response == nullptr);
			WebAuthn::OperationRequestVerifier::VerifyOperationRequest(*request);

            auto ipcRequest = WebAuthn::RequestDecoder::DecodeMakeCredential(request);

            auto credential = m_passkeyClient->MakeCredential(std::move(ipcRequest)).get();
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

			auto assertion = m_passkeyClient->GetAssertion(std::move(ipcRequest)).get();
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

			m_appHostLauncher->ActivateHeadless();

			*lockStatus = PluginLocked;
			return S_OK;
		}
		CATCH_RETURN();
	}

}

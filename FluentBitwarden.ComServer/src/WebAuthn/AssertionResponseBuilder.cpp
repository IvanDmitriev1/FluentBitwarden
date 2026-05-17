#include "pch.h"
#include "AssertionResponseBuilder.h"

namespace FluentBitwarden::ComServer::WebAuthn::AssertionResponseBuilder
{
	void BuildResponse(const PasskeyAssertionResponse& assertion, PWEBAUTHN_PLUGIN_OPERATION_RESPONSE response)
	{
		THROW_HR_IF_NULL(E_POINTER, response);
		THROW_HR_IF(E_INVALIDARG, assertion.CredentialId.empty());
		THROW_HR_IF(E_INVALIDARG, assertion.UserId.empty());
		THROW_HR_IF(E_INVALIDARG, assertion.AuthenticatorData.empty());
		THROW_HR_IF(E_INVALIDARG, assertion.Signature.empty());
		THROW_HR_IF(E_INVALIDARG, assertion.UserName.empty());
		THROW_HR_IF(E_INVALIDARG, assertion.UserDisplayName.empty());

		WEBAUTHN_CREDENTIAL credential{};
		credential.dwVersion = WEBAUTHN_CREDENTIAL_CURRENT_VERSION;
		credential.cbId = static_cast<DWORD>(assertion.CredentialId.size());
		credential.pbId = const_cast<PBYTE>(assertion.CredentialId.data());
		credential.pwszCredentialType = WEBAUTHN_CREDENTIAL_TYPE_PUBLIC_KEY;

		WEBAUTHN_ASSERTION webAuthnAssertion{};
		webAuthnAssertion.Credential = credential;
		webAuthnAssertion.dwVersion = WEBAUTHN_ASSERTION_CURRENT_VERSION;

		webAuthnAssertion.cbAuthenticatorData = static_cast<DWORD>(assertion.AuthenticatorData.size());
		webAuthnAssertion.pbAuthenticatorData = const_cast<PBYTE>(assertion.AuthenticatorData.data());

		webAuthnAssertion.cbSignature = static_cast<DWORD>(assertion.Signature.size());
		webAuthnAssertion.pbSignature = const_cast<PBYTE>(assertion.Signature.data());

		webAuthnAssertion.cbUserId = static_cast<DWORD>(assertion.UserId.size());
		webAuthnAssertion.pbUserId = const_cast<PBYTE>(assertion.UserId.data());

		WEBAUTHN_USER_ENTITY_INFORMATION userInfo{};
		userInfo.dwVersion = WEBAUTHN_USER_ENTITY_INFORMATION_CURRENT_VERSION;
		userInfo.cbId = static_cast<DWORD>(assertion.UserId.size());
		userInfo.pbId = const_cast<PBYTE>(assertion.UserId.data());

		const auto userName = winrt::to_hstring(assertion.UserName);
		const auto userDisplayName = winrt::to_hstring(assertion.UserDisplayName);

		userInfo.pwszName = userName.c_str();
		userInfo.pwszDisplayName = userDisplayName.c_str();
		userInfo.pwszIcon = nullptr;

		WEBAUTHN_CTAPCBOR_GET_ASSERTION_RESPONSE getAssertionResponse{};
		getAssertionResponse.WebAuthNAssertion = webAuthnAssertion;

		// Optional CTAP [4] user field.
		getAssertionResponse.pUserInformation = &userInfo;

		// Optional.
		getAssertionResponse.dwNumberOfCredentials = 0;
		getAssertionResponse.lUserSelected = 0;
		getAssertionResponse.cbLargeBlobKey = 0;
		getAssertionResponse.pbLargeBlobKey = nullptr;
		getAssertionResponse.cbUnsignedExtensionOutputs = 0;
		getAssertionResponse.pbUnsignedExtensionOutputs = nullptr;

		DWORD cbEncodedResponse = 0;
		BYTE* pbEncodedResponse = nullptr;
		THROW_IF_FAILED(WebAuthNEncodeGetAssertionResponse(
			&getAssertionResponse,
			&cbEncodedResponse,
			&pbEncodedResponse));

		THROW_HR_IF(E_UNEXPECTED, cbEncodedResponse == 0 || pbEncodedResponse == nullptr);

		response->cbEncodedResponse = cbEncodedResponse;
		response->pbEncodedResponse = pbEncodedResponse;
	}
}

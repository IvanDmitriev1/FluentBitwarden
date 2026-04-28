#include "pch.h"
#include "DecodedWebAuthnGetAssertionRequest.h"
#include "Utils/HashingFunctions.h"

namespace FluentBitwarden::ComServer::WebAuthn
{
	DecodedGetAssertionRequest::DecodedGetAssertionRequest(PWEBAUTHN_CTAPCBOR_GET_ASSERTION_REQUEST request) noexcept
		:m_request(request)
	{
	}

	HRESULT DecodedGetAssertionRequest::Decode(PCWEBAUTHN_PLUGIN_OPERATION_REQUEST request, DecodedGetAssertionRequest& result) noexcept
	{
		auto encodedRequest = std::span<const std::uint8_t>(
			reinterpret_cast<const std::uint8_t*>(request->pbEncodedRequest),
			request->cbEncodedRequest);

		PWEBAUTHN_CTAPCBOR_GET_ASSERTION_REQUEST rawRequest = nullptr;
		RETURN_IF_FAILED(WebAuthNDecodeGetAssertionRequest(
			static_cast<DWORD>(encodedRequest.size()),
			encodedRequest.data(),
			&rawRequest));

		result = DecodedGetAssertionRequest(rawRequest);
		return S_OK;
	}


	HRESULT DecodedGetAssertionRequest::ToIpcRequest(PasskeyGetAssertionRequest& result) const noexcept
	{
		RETURN_HR_IF_NULL(E_UNEXPECTED, m_request.get());
		const auto* request = m_request.get();

		std::vector<std::uint8_t> rpIdHash;
		RETURN_IF_FAILED(Utils::ComputeSha256(
			std::span<const std::uint8_t>(
			reinterpret_cast<const std::uint8_t*>(request->pbRpId),
			request->cbRpId),
			rpIdHash));

		RETURN_HR_IF(E_INVALIDARG, rpIdHash.size() != 32);
		RETURN_HR_IF(E_INVALIDARG, request->cbClientDataHash != 32);

		std::vector<std::uint8_t> clientDataHash(
			request->pbClientDataHash,
			request->pbClientDataHash + request->cbClientDataHash);

		result = PasskeyGetAssertionRequest
		{
			.RpId = winrt::hstring(request->pwszRpId),
			.RpIdHash = std::move(rpIdHash),
			.ClientDataHash = std::move(clientDataHash)
		};

		return S_OK;
	}
}
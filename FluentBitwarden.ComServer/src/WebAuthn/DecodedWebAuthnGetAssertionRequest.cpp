#include "pch.h"
#include "DecodedWebAuthnGetAssertionRequest.h"
#include "Utils/HashingFunctions.h"

namespace FluentBitwarden::ComServer::WebAuthn
{
	DecodedGetAssertionRequest::DecodedGetAssertionRequest(PWEBAUTHN_CTAPCBOR_GET_ASSERTION_REQUEST request) noexcept
		:m_request(request)
	{
	}

	DecodedGetAssertionRequest DecodedGetAssertionRequest::Decode(PCWEBAUTHN_PLUGIN_OPERATION_REQUEST request)
	{
		THROW_HR_IF_NULL(E_POINTER, request);

		auto encodedRequest = std::span<const std::uint8_t>(
			reinterpret_cast<const std::uint8_t*>(request->pbEncodedRequest),
			request->cbEncodedRequest);

		PWEBAUTHN_CTAPCBOR_GET_ASSERTION_REQUEST rawRequest = nullptr;
		THROW_IF_FAILED(WebAuthNDecodeGetAssertionRequest(
			static_cast<DWORD>(encodedRequest.size()),
			encodedRequest.data(),
			&rawRequest));

		return DecodedGetAssertionRequest(rawRequest);
	}


	PasskeyGetAssertionRequest DecodedGetAssertionRequest::ToIpcRequest() const
	{
		THROW_HR_IF_NULL(E_UNEXPECTED, m_request.get());
		const auto* request = m_request.get();

		std::vector<std::uint8_t> rpIdHash = Utils::ComputeSha256(
			std::span<const std::uint8_t>(
			reinterpret_cast<const std::uint8_t*>(request->pbRpId),
			request->cbRpId));

		THROW_HR_IF(E_INVALIDARG, rpIdHash.size() != 32);
		THROW_HR_IF(E_INVALIDARG, request->cbClientDataHash != 32);

		std::vector<std::uint8_t> clientDataHash(
			request->pbClientDataHash,
			request->pbClientDataHash + request->cbClientDataHash);

		return PasskeyGetAssertionRequest
		{
			.RpId = winrt::hstring(request->pwszRpId),
			.RpIdHash = std::move(rpIdHash),
			.ClientDataHash = std::move(clientDataHash)
		};
	}
}

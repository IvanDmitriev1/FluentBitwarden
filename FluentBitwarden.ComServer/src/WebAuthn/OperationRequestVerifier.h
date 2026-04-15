#pragma once
#include "pch.h"

class OperationRequestVerifier final
{
public:
	static HRESULT VerifyOperationRequest(const WEBAUTHN_PLUGIN_OPERATION_REQUEST& request) noexcept;
	static HRESULT VerifyCancelRequest(const WEBAUTHN_PLUGIN_CANCEL_OPERATION_REQUEST& request) noexcept;

private:
	static HRESULT ComputeSha256(
		std::span<const std::uint8_t> data,
		std::vector<std::uint8_t>& hash) noexcept;

	static HRESULT VerifySignedBuffer(
		std::span<const std::uint8_t> signedBuffer,
		std::span<const std::uint8_t> signature,
		std::span<const std::uint8_t> publicKeyBlob) noexcept;
};


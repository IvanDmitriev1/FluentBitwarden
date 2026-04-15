#include "pch.h"
#include "OperationRequestVerifier.h"
#include "Authenticator/PluginRegistrationManager.h"

#include <bcrypt.h>
#include <ncrypt.h>

using unique_bcrypt_hash = wil::unique_any<
	BCRYPT_HASH_HANDLE,
	decltype(&BCryptDestroyHash),
	BCryptDestroyHash>;

using unique_bcrypt_alg = wil::unique_any<
	BCRYPT_ALG_HANDLE,
	decltype(&BCryptCloseAlgorithmProvider),
	BCryptCloseAlgorithmProvider>;

using unique_ncrypt_prov = wil::unique_any<
	NCRYPT_PROV_HANDLE,
	decltype(&NCryptFreeObject),
	NCryptFreeObject>;

using unique_ncrypt_key = wil::unique_any<
	NCRYPT_KEY_HANDLE,
	decltype(&NCryptFreeObject),
	NCryptFreeObject>;

HRESULT OperationRequestVerifier::VerifyOperationRequest(const WEBAUTHN_PLUGIN_OPERATION_REQUEST& request) noexcept
{
	RETURN_HR_IF(E_INVALIDARG, request.requestType != WEBAUTHN_PLUGIN_REQUEST_TYPE_CTAP2_CBOR);
	RETURN_HR_IF(E_INVALIDARG, request.cbEncodedRequest == 0 || request.pbEncodedRequest == nullptr);
	RETURN_HR_IF(E_INVALIDARG, request.cbRequestSignature == 0 || request.pbRequestSignature == nullptr);

	std::vector<std::uint8_t> publicKeyBlob;
	RETURN_IF_FAILED(PluginRegistrationManager::GetOperationSigningPublicKey(publicKeyBlob));

	return VerifySignedBuffer(
		std::span<const std::uint8_t>(
		reinterpret_cast<const std::uint8_t*>(request.pbEncodedRequest),
		request.cbEncodedRequest),
		std::span<const std::uint8_t>(
		reinterpret_cast<const std::uint8_t*>(request.pbRequestSignature),
		request.cbRequestSignature),
		publicKeyBlob);
}

HRESULT OperationRequestVerifier::VerifyCancelRequest(const WEBAUTHN_PLUGIN_CANCEL_OPERATION_REQUEST& request) noexcept
{
	RETURN_HR_IF(E_INVALIDARG, request.cbRequestSignature == 0 || request.pbRequestSignature == nullptr);

	UNREFERENCED_PARAMETER(request);
	return HRESULT_FROM_WIN32(ERROR_CALL_NOT_IMPLEMENTED);
}

HRESULT OperationRequestVerifier::ComputeSha256(std::span<const std::uint8_t> data, std::vector<std::uint8_t>& hash) noexcept
{
	hash.clear();

	DWORD objectLength = 0;
	DWORD hashLength = 0;
	DWORD bytesRead = 0;

	RETURN_IF_NTSTATUS_FAILED(BCryptGetProperty(
		BCRYPT_SHA256_ALG_HANDLE,
		BCRYPT_OBJECT_LENGTH,
		reinterpret_cast<PUCHAR>(&objectLength),
		sizeof(objectLength),
		&bytesRead,
		0));

	RETURN_IF_NTSTATUS_FAILED(BCryptGetProperty(
		BCRYPT_SHA256_ALG_HANDLE,
		BCRYPT_HASH_LENGTH,
		reinterpret_cast<PUCHAR>(&hashLength),
		sizeof(hashLength),
		&bytesRead,
		0));


	auto hashObject = wil::make_unique_cotaskmem<std::uint8_t[]>(objectLength);
	RETURN_HR_IF_NULL(E_OUTOFMEMORY, hashObject);

	unique_bcrypt_hash hashHandle{};
	RETURN_IF_NTSTATUS_FAILED(BCryptCreateHash(
		BCRYPT_SHA256_ALG_HANDLE,
		wil::out_param(hashHandle),
		hashObject.get(),
		objectLength,
		nullptr,
		0,
		0));

	RETURN_IF_NTSTATUS_FAILED(BCryptHashData(
		hashHandle.get(),
		const_cast<PUCHAR>(reinterpret_cast<const UCHAR*>(data.data())),
		static_cast<ULONG>(data.size()),
		0));


	hash.resize(hashLength);
	RETURN_IF_NTSTATUS_FAILED(BCryptFinishHash(
		hashHandle.get(),
		reinterpret_cast<PUCHAR>(hash.data()),
		hashLength,
		0));

	return S_OK;
}

HRESULT OperationRequestVerifier::VerifySignedBuffer(
	std::span<const std::uint8_t> signedBuffer,
	std::span<const std::uint8_t> signature,
	std::span<const std::uint8_t> publicKeyBlob) noexcept
{
	RETURN_HR_IF(E_INVALIDARG, signedBuffer.empty());
	RETURN_HR_IF(E_INVALIDARG, signature.empty());
	RETURN_HR_IF(E_INVALIDARG, publicKeyBlob.empty());


	unique_ncrypt_prov provider{};
	unique_ncrypt_key publicKey{};

	RETURN_IF_FAILED(NCryptOpenStorageProvider(&provider, nullptr, 0));
	RETURN_IF_FAILED(NCryptImportKey(
		provider.get(),
		0,
		BCRYPT_PUBLIC_KEY_BLOB,
		nullptr,
		&publicKey,
		const_cast<PBYTE>(publicKeyBlob.data()),
		static_cast<DWORD>(publicKeyBlob.size()),
		0));

	std::vector<std::uint8_t> hash;
	RETURN_IF_FAILED(ComputeSha256(signedBuffer, hash));

	void* paddingInfo = nullptr;
	DWORD flags = 0;

	// For RSA public-key blobs, NCryptVerifySignature requires PKCS#1 padding info.
	// For ECC, flags stay 0.
	RETURN_HR_IF(E_INVALIDARG, publicKeyBlob.size() < sizeof(BCRYPT_KEY_BLOB));
	auto* keyBlob = reinterpret_cast<const BCRYPT_KEY_BLOB*>(publicKeyBlob.data());

	BCRYPT_PKCS1_PADDING_INFO rsaPaddingInfo{};
	if (keyBlob->Magic == BCRYPT_RSAPUBLIC_MAGIC)
	{
		rsaPaddingInfo.pszAlgId = BCRYPT_SHA256_ALGORITHM;
		paddingInfo = &rsaPaddingInfo;
		flags = BCRYPT_PAD_PKCS1;
	}

	RETURN_IF_WIN32_ERROR(NCryptVerifySignature(
		publicKey.get(),
		paddingInfo,
		hash.data(),
		static_cast<DWORD>(hash.size()),
		const_cast<PBYTE>(signature.data()),
		static_cast<DWORD>(signature.size()),
		flags));

	return S_OK;
}

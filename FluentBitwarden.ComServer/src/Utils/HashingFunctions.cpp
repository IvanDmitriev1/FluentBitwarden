#include "pch.h"
#include "HashingFunctions.h"

#include <bcrypt.h>

namespace FluentBitwarden::ComServer::Utils
{
	using unique_bcrypt_hash = wil::unique_any<
		BCRYPT_HASH_HANDLE,
		decltype(&BCryptDestroyHash),
		BCryptDestroyHash>;


	HRESULT ComputeSha256(std::span<const std::uint8_t> data, std::vector<std::uint8_t>& hash) noexcept
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
}
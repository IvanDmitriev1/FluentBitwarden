#include <pch.h>
#include "Base64Url.h"


namespace FluentBitwarden::ComServer::Utils
{
	winrt::hstring Base64UrlEncode(std::span<const std::uint8_t> bytes)
	{
		const auto base64 = CryptographicBuffer::EncodeToBase64String(
			CryptographicBuffer::CreateFromByteArray(winrt::array_view<const std::uint8_t>{ bytes }));

		std::wstring base64Url{ base64.c_str(), base64.size() };
		for (auto& value : base64Url)
		{
			if (value == L'+')
			{
				value = L'-';
			}
			else if (value == L'/')
			{
				value = L'_';
			}
		}

		while (!base64Url.empty() && base64Url.back() == L'=')
		{
			base64Url.pop_back();
		}

		return winrt::hstring{ base64Url };
	}

	std::vector<std::uint8_t> Base64UrlDecode(const winrt::hstring& encoded)
	{
		const std::wstring_view base64Url{ encoded.c_str(), encoded.size() };

		std::wstring base64;
		base64.reserve(base64Url.size() + 2);

		std::size_t padding = 0;
		for (const auto value : base64Url)
		{
			if (value == L'=')
			{
				if (++padding > 2)
				{
					throw std::invalid_argument("Invalid Base64Url padding.");
				}

				continue;
			}

			if (padding > 0)
			{
				throw std::invalid_argument("Invalid Base64Url padding.");
			}

			if (value == L'-')
			{
				base64.push_back(L'+');
			}
			else if (value == L'_')
			{
				base64.push_back(L'/');
			}
			else if ((value >= L'A' && value <= L'Z') ||
					 (value >= L'a' && value <= L'z') ||
					 (value >= L'0' && value <= L'9'))
			{
				base64.push_back(value);
			}
			else
			{
				throw std::invalid_argument("Invalid Base64Url character.");
			}
		}

		const auto remainder = base64.size() % 4;
		if (remainder == 1)
		{
			throw std::invalid_argument("Invalid Base64Url length.");
		}

		if (padding > 0 &&
			(base64Url.size() % 4 != 0 ||
			remainder == 0 ||
			padding != 4 - remainder))
		{
			throw std::invalid_argument("Invalid Base64Url padding.");
		}

		if (remainder > 0)
		{
			base64.append(4 - remainder, L'=');
		}

		const auto buffer = CryptographicBuffer::DecodeFromBase64String(winrt::hstring{ base64 });
		winrt::com_array<std::uint8_t> bytes;
		CryptographicBuffer::CopyToByteArray(buffer, bytes);

		return { bytes.begin(), bytes.end() };
	}
}
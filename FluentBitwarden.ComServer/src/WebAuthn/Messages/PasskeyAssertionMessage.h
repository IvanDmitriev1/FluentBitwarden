#pragma once
#include <pch.h>
#include "Utils/Base64Url.h"
#include "Ipc/IpcProtocol.h"

namespace FluentBitwarden::ComServer::WebAuthn
{
	struct PasskeyGetAssertionRequest
	{
		winrt::hstring RpId;
		std::vector<std::uint8_t> RpIdHash;
		std::vector<std::uint8_t> ClientDataHash;

		static constexpr std::uint16_t MessageType = 2;

		[[nodiscard]] JsonObject ToJson() const
		{
			JsonObject json;
			json.SetNamedValue(L"RpId", JsonValue::CreateStringValue(RpId));
			json.SetNamedValue(L"RpIdHash", JsonValue::CreateStringValue(Utils::Base64UrlEncode(RpIdHash)));
			json.SetNamedValue(L"ClientDataHash", JsonValue::CreateStringValue(Utils::Base64UrlEncode(ClientDataHash)));
			return json;
		}
	};

	struct PasskeyAssertionResponse
	{
		std::vector<std::uint8_t> CredentialId;
		std::vector<std::uint8_t> UserId;
		std::vector<std::uint8_t> AuthenticatorData;
		std::vector<std::uint8_t> Signature;
		winrt::hstring UserName;
		winrt::hstring UserDisplayName;

		[[nodiscard]] static PasskeyAssertionResponse FromJson(const JsonObject& json)
		{
			return PasskeyAssertionResponse
			{
				.CredentialId = Utils::Base64UrlDecode(json.GetNamedString(L"CredentialId")),
				.UserId = Utils::Base64UrlDecode(json.GetNamedString(L"UserId")),
				.AuthenticatorData = Utils::Base64UrlDecode(json.GetNamedString(L"AuthenticatorData")),
				.Signature = Utils::Base64UrlDecode(json.GetNamedString(L"Signature")),
				.UserName = json.GetNamedString(L"UserName"),
				.UserDisplayName = json.GetNamedString(L"UserDisplayName")
			};
		}
	};

	static_assert(Ipc::IpcJsonRequest<PasskeyGetAssertionRequest>);
	static_assert(Ipc::IpcJsonResponse<PasskeyAssertionResponse>);
}

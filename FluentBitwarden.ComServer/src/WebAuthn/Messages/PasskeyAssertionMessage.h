#pragma once
#include <pch.h>
#include "Ipc/Base64Url.h"
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
			json.SetNamedValue(L"RpIdHash", JsonValue::CreateStringValue(Ipc::Base64UrlEncode(RpIdHash)));
			json.SetNamedValue(L"ClientDataHash", JsonValue::CreateStringValue(Ipc::Base64UrlEncode(ClientDataHash)));
			return json;
		}
	};

	struct PasskeyAssertionResponse
	{
		std::vector<std::uint8_t> CredentialId;
		std::vector<std::uint8_t> UserId;
		std::vector<std::uint8_t> AuthenticatorData;
		std::vector<std::uint8_t> Signature;

		[[nodiscard]] static PasskeyAssertionResponse FromJson(const JsonObject& json)
		{
			return PasskeyAssertionResponse
			{
				.CredentialId = Ipc::Base64UrlDecode(json.GetNamedString(L"CredentialId")),
				.UserId = Ipc::Base64UrlDecode(json.GetNamedString(L"UserId")),
				.AuthenticatorData = Ipc::Base64UrlDecode(json.GetNamedString(L"AuthenticatorData")),
				.Signature = Ipc::Base64UrlDecode(json.GetNamedString(L"Signature"))
			};
		}
	};

	static_assert(Ipc::IpcJsonRequest<PasskeyGetAssertionRequest>);
	static_assert(Ipc::IpcJsonResponse<PasskeyAssertionResponse>);
}

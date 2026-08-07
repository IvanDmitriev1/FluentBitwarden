#pragma once
#include "Passkey/WebAuthn/WebAuthnPayload.h"
#include "Infrastructure/Ipc/IpcBinary.h"
#include "Infrastructure/Ipc/IpcProtocol.h"

namespace FluentBitwarden::ComServer::WebAuthn
{
	struct PasskeyGetAssertionRequest
	{
		std::string RpId;
		std::vector<std::uint8_t> RpIdHash;
		std::vector<std::uint8_t> ClientDataHash;

		static constexpr std::uint16_t MessageType = 50;

		[[nodiscard]] std::vector<std::byte> ToPayload() const
		{
			const auto rpIdHash = std::span<const std::uint8_t>{ RpIdHash.data(), RpIdHash.size() };
			const auto clientDataHash = std::span<const std::uint8_t>{ ClientDataHash.data(), ClientDataHash.size() };

            Payload::ValidateSha256Hash(rpIdHash, "RpIdHash");
            Payload::ValidateSha256Hash(clientDataHash, "ClientDataHash");

			Ipc::Binary::PayloadWriter writer;
			writer.WriteObjectHeader(MemberCount);
			writer.WriteString(RpId);
			writer.WriteBytes(rpIdHash);
			writer.WriteBytes(clientDataHash);
			return std::move(writer).Take();
		}

    private:
        static constexpr std::uint8_t MemberCount = 3;
	};

	struct PasskeyAssertionResponse
	{
		std::vector<std::uint8_t> CredentialId;
		std::vector<std::uint8_t> UserId;
		std::vector<std::uint8_t> AuthenticatorData;
		std::vector<std::uint8_t> Signature;
		std::string UserName;
		std::string UserDisplayName;

		[[nodiscard]] static PasskeyAssertionResponse FromPayload(std::span<const std::byte> payload)
		{
			Ipc::Binary::PayloadReader reader{ payload };
			reader.ReadObjectHeader(1);
			reader.ReadObjectHeader(MemberCount);

			auto credentialId = reader.ReadBytes();
			auto userId = reader.ReadBytes();
			auto authenticatorData = reader.ReadBytes();
			auto signature = reader.ReadBytes();
			auto userName = reader.ReadString();
			auto userDisplayName = reader.ReadString();
			reader.EnsureConsumed();

			return PasskeyAssertionResponse
			{
				.CredentialId = std::move(credentialId),
				.UserId = std::move(userId),
				.AuthenticatorData = std::move(authenticatorData),
				.Signature = std::move(signature),
				.UserName = std::move(userName),
				.UserDisplayName = std::move(userDisplayName)
			};
		}

    private:
        static constexpr std::uint8_t MemberCount = 6;
	};

	static_assert(Ipc::IpcBinaryRequest<PasskeyGetAssertionRequest>);
	static_assert(Ipc::IpcBinaryResponse<PasskeyAssertionResponse>);
}

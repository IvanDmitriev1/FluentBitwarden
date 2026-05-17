#pragma once
#include <pch.h>
#include "Ipc/IpcBinary.h"
#include "Ipc/IpcProtocol.h"

namespace FluentBitwarden::ComServer::WebAuthn
{
	namespace PasskeyPayload
	{
		inline constexpr std::uint16_t SchemaVersion = 1;
		inline constexpr std::size_t Sha256HashLength = 32;

		inline void ValidateSchemaVersion(std::uint16_t schemaVersion, std::string_view payloadName)
		{
			if (schemaVersion != SchemaVersion)
			{
				throw std::runtime_error(std::string{ "Unsupported " } + std::string{ payloadName } + " schema version.");
			}
		}

		inline void ValidateHashLength(std::span<const std::uint8_t> value, std::string_view fieldName)
		{
			if (value.size() != Sha256HashLength)
			{
				throw std::runtime_error(std::string{ fieldName } + " must be 32 bytes.");
			}
		}
	}

	struct PasskeyGetAssertionRequest
	{
		std::string RpId;
		std::vector<std::uint8_t> RpIdHash;
		std::vector<std::uint8_t> ClientDataHash;

		static constexpr std::uint16_t MessageType = 2;

		[[nodiscard]] std::vector<std::byte> ToPayload() const
		{
			const auto rpIdHash = std::span<const std::uint8_t>{ RpIdHash.data(), RpIdHash.size() };
			const auto clientDataHash = std::span<const std::uint8_t>{ ClientDataHash.data(), ClientDataHash.size() };

			PasskeyPayload::ValidateHashLength(rpIdHash, "RpIdHash");
			PasskeyPayload::ValidateHashLength(clientDataHash, "ClientDataHash");

			Ipc::Binary::PayloadWriter writer;
			writer.WriteUInt16(PasskeyPayload::SchemaVersion);
			writer.WriteString(RpId);
			writer.WriteBytes(rpIdHash);
			writer.WriteBytes(clientDataHash);
			return std::move(writer).Take();
		}
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
			PasskeyPayload::ValidateSchemaVersion(reader.ReadUInt16(), "passkey assertion response");

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
	};

	static_assert(Ipc::IpcBinaryRequest<PasskeyGetAssertionRequest>);
	static_assert(Ipc::IpcBinaryResponse<PasskeyAssertionResponse>);
}

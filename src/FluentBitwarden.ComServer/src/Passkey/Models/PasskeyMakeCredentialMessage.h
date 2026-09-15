#pragma once
#include "Passkey/WebAuthn/WebAuthnPayload.h"
#include "Infrastructure/Ipc/IpcBinary.h"
#include "Infrastructure/Ipc/IpcProtocol.h"

namespace FluentBitwarden::ComServer::WebAuthn
{
    struct PasskeyMakeCredentialRequest
    {
        std::string RpId;
        std::string RpName;
        std::vector<std::uint8_t> RpIdHash;
        std::vector<std::uint8_t> ClientDataHash;
        std::vector<std::uint8_t> UserId;
        std::string UserName;
        std::string UserDisplayName;
        bool RequireResidentKey;
        bool UserVerification;

        static constexpr std::uint16_t MessageType = 51;

        [[nodiscard]] std::vector<std::byte> ToPayload() const
        {
            const auto rpIdHash = std::span<const std::uint8_t>{ RpIdHash.data(), RpIdHash.size() };
            const auto clientDataHash = std::span<const std::uint8_t>{ ClientDataHash.data(), ClientDataHash.size() };
            const auto userId = std::span<const std::uint8_t>{ UserId.data(),UserId.size() };

            Payload::ValidateSha256Hash(rpIdHash,"RpIdHash");
            Payload::ValidateSha256Hash(clientDataHash,"ClientDataHash");

            Ipc::Binary::PayloadWriter writer;
            writer.WriteObjectHeader(MemberCount);

            writer.WriteString(RpId);
            writer.WriteString(RpName);
            writer.WriteBytes(rpIdHash);
            writer.WriteBytes(clientDataHash);
            writer.WriteBytes(userId);
            writer.WriteString(UserName);
            writer.WriteString(UserDisplayName);
            writer.WriteBool(RequireResidentKey);
            writer.WriteBool(UserVerification);

            return std::move(writer).Take();
        }

    private:
        static constexpr std::uint8_t MemberCount = 9;
    };

    struct PasskeyMakeCredentialResponse
    {
        std::vector<std::uint8_t> AuthenticatorData;

        [[nodiscard]] static PasskeyMakeCredentialResponse FromPayload(std::span<const std::byte> payload)
        {
            Ipc::Binary::PayloadReader reader{ payload };

            reader.ReadObjectHeader(1);
            reader.ReadObjectHeader(MemberCount);

            auto authenticatorData = reader.ReadBytes();

            reader.EnsureConsumed();

            return PasskeyMakeCredentialResponse
            {
                .AuthenticatorData = std::move(authenticatorData),
            };
        }

    private:
        static constexpr std::uint8_t MemberCount = 1;
    };

    static_assert(Ipc::IpcBinaryRequest<PasskeyMakeCredentialRequest>);
    static_assert(Ipc::IpcBinaryResponse<PasskeyMakeCredentialResponse>);
}

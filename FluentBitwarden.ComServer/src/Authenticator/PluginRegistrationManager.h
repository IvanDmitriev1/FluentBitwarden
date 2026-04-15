#pragma once
#include <pch.h>

class PluginRegistrationManager final
{
    static constexpr std::wstring_view kPluginName = L"FluentBitwarden";
    static constexpr std::wstring_view kPluginRpId = L"fluentbitwarden.app";

public:
    static HRESULT EnsureRegistered() noexcept;
    static HRESULT Unregister() noexcept;

    static HRESULT GetOperationSigningPublicKey(
        std::vector<std::uint8_t>& publicKey) noexcept;

private:
    static HRESULT RegisterNew() noexcept;
    static HRESULT UpdateExisting() noexcept;
    static HRESULT IsRegistered(bool& registered) noexcept;
};

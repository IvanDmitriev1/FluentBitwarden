#pragma once
#include <pch.h>

namespace FluentBitwarden::ComServer
{

    class PluginRegistrationManager final
    {
        static constexpr std::wstring_view kPluginName = L"FluentBitwarden";
        static constexpr std::wstring_view kPluginRpId = L"fluentbitwarden.app";

    public:
        static void EnsureRegistered();
        static void Unregister();

        static std::vector<std::uint8_t> GetOperationSigningPublicKey();

    private:
        static void RegisterNew();
        static void UpdateExisting();
        static bool IsRegistered();
    };

}

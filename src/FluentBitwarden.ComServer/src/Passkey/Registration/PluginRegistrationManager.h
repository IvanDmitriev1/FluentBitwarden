#pragma once
#include <pch.h>

namespace FluentBitwarden::ComServer::PluginRegistrationManager
{
    void EnsureRegistered();
    void Unregister();
    std::vector<std::uint8_t> GetOperationSigningPublicKey();
}

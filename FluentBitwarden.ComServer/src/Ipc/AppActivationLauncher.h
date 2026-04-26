#pragma once
#include <pch.h>

namespace FluentBitwarden::ComServer::Ipc::AppActivationLauncher
{
    IAsyncAction ActivateMainApp(std::wstring launchArguments);
}

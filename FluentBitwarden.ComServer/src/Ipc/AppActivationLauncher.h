#pragma once

class AppActivationLauncher final
{
public:
    static HRESULT ActivateMainApp(const std::wstring& launchArguments) noexcept;
};


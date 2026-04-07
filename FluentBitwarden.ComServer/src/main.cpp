#include "pch.h"
#include <winrt/base.h>
#include <winrt/Windows.System.h>
#include <winrt/Windows.ApplicationModel.h>
#include <winrt/Windows.Foundation.Metadata.h>
#include "PluginAuthenticator/PluginAuthenticator.h"
#include "PluginAuthenticator/PluginRegistrationManager.h"

using namespace winrt::Windows::Foundation;
using namespace winrt::Windows::System;
using namespace winrt::Windows::ApplicationModel;
using namespace winrt::Windows::Foundation::Metadata;


int RunComServer();
int RegisterPlugin();

void AttachDebugger()
{
    std::string cmd = "vsjitdebugger.exe -p " + std::to_string(GetCurrentProcessId());
    system(cmd.c_str());
}


int WINAPI wWinMain(HINSTANCE, HINSTANCE, PWSTR pCmdLine, int)
{
#ifdef _DEBUG
    AttachDebugger();
#endif

    std::wstring args = pCmdLine ? pCmdLine : L"";

    if (args.find(L"-PluginActivated") != std::wstring::npos)
    {
        return RunComServer();
    }

    if (args.find(L"-Register") != std::wstring::npos)
    {
        return RegisterPlugin();
    }
    
    return 1;
}

int RunComServer()
{
    winrt::init_apartment(winrt::apartment_type::single_threaded);

    winrt::check_hresult(
        CoInitializeSecurity(
        nullptr,
        -1,
        nullptr,
        nullptr,
        RPC_C_AUTHN_LEVEL_DEFAULT,
        RPC_C_IMP_LEVEL_IMPERSONATE,
        nullptr,
        EOAC_NONE,
        nullptr));

    auto factory = winrt::make<PluginAuthenticatorFactory>();

    DWORD registrationToken{};
    winrt::check_hresult(CoRegisterClassObject(
        PluginAuthenticatorImpl::CLSID,
        factory.get(),
        CLSCTX_LOCAL_SERVER,
        REGCLS_MULTIPLEUSE,
        &registrationToken));

    MSG msg{};
    while (GetMessageW(&msg, nullptr, 0, 0))
    {
        TranslateMessage(&msg);
        DispatchMessageW(&msg);
    }

    CoRevokeClassObject(registrationToken);
    winrt::uninit_apartment();
    return 0;
}

int RegisterPlugin()
{
    winrt::init_apartment(winrt::apartment_type::single_threaded);
    
    winrt::check_hresult(
        CoInitializeSecurity(
        nullptr,
        -1,
        nullptr,
        nullptr,
        RPC_C_AUTHN_LEVEL_DEFAULT,
        RPC_C_IMP_LEVEL_IMPERSONATE,
        nullptr,
        EOAC_NONE,
        nullptr));

    auto& instance = PluginRegistrationManager::GetInstance();
    HRESULT result = instance.RegisterPlugin();

    winrt::uninit_apartment();
    return result;
}

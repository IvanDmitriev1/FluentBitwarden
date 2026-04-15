#include "pch.h"
#include "Authenticator/PluginAuthenticator.h"
#include "Authenticator/PluginRegistrationManager.h"

static void AttachDebugger()
{
    std::string cmd = "vsjitdebugger.exe -p " + std::to_string(GetCurrentProcessId());
    system(cmd.c_str());

    DebugBreak();
}

static int RunPluginComServer() noexcept
{
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
        PluginAuthenticator::CLSID,
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
    return 0;
}

int WINAPI wWinMain(HINSTANCE, HINSTANCE, PWSTR pCmdLine, int)
{
    winrt::init_apartment(winrt::apartment_type::single_threaded);

#ifdef _DEBUG
    if (!IsDebuggerPresent())
    {
        AttachDebugger();
    }
#endif

    std::wstring args = pCmdLine ? pCmdLine : L"";
    if (args.find(L"-PluginActivated") != std::wstring::npos)
    {
        return RunPluginComServer();
    }

    if (args.find(L"--register-plugin") != std::wstring::npos)
    {
        return PluginRegistrationManager::EnsureRegistered();
    }

    if (args.find(L"--unregister-plugin") != std::wstring::npos)
    {
        return PluginRegistrationManager::Unregister();
    }

    return 1;
}

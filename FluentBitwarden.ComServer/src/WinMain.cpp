#include "pch.h"
#include "Authenticator/PluginAuthenticator.h"
#include "Authenticator/PluginRegistrationManager.h"

static void AttachDebugger()
{
    try
    {
        std::string cmd = "vsjitdebugger.exe -p " + std::to_string(GetCurrentProcessId());
        system(cmd.c_str());

        DebugBreak();
    }
    catch (...)
    {

    }
}

static HRESULT RunPluginComServer() noexcept
{
    try
    {
        RETURN_IF_FAILED(
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

        auto factory = winrt::make<FluentBitwarden::ComServer::PluginAuthenticatorFactory>();

        DWORD registrationToken{};
        RETURN_IF_FAILED(CoRegisterClassObject(
            FluentBitwarden::ComServer::PluginAuthenticator::CLSID,
            factory.get(),
            CLSCTX_LOCAL_SERVER,
            REGCLS_MULTIPLEUSE,
            &registrationToken));

        auto revokeRegistration = wil::scope_exit([&]
        {
            CoRevokeClassObject(registrationToken);
        });

        MSG msg{};
        BOOL getMessageResult = 0;

        while ((getMessageResult = GetMessageW(&msg, nullptr, 0, 0)) > 0)
        {
            TranslateMessage(&msg);
            DispatchMessageW(&msg);
        }

        RETURN_LAST_ERROR_IF(getMessageResult == -1);

        const HRESULT revokeResult = CoRevokeClassObject(registrationToken);
        revokeRegistration.release();
        RETURN_IF_FAILED(revokeResult);

        return S_OK;
    }
    CATCH_RETURN();
}

int WINAPI wWinMain(HINSTANCE, HINSTANCE, PWSTR pCmdLine, int)
{
    winrt::init_apartment(winrt::apartment_type::multi_threaded);

    /*#ifdef _DEBUG
            if (!IsDebuggerPresent())
            {
                FluentBitwarden::ComServer::AttachDebugger();
            }
    #endif*/

    std::wstring args = pCmdLine ? pCmdLine : L"";
    if (args.find(L"-PluginActivated") != std::wstring::npos)
    {
        return RunPluginComServer();
    }

    if (args.find(L"--register-plugin") != std::wstring::npos)
    {
        FluentBitwarden::ComServer::PluginRegistrationManager::EnsureRegistered();
        return 0;
    }

    if (args.find(L"--unregister-plugin") != std::wstring::npos)
    {
        FluentBitwarden::ComServer::PluginRegistrationManager::Unregister();
        return 0;
    }

    return 1;
}

#include "pch.h"
#include "AppActivationLauncher.h"

namespace FluentBitwarden::ComServer::AppActivationLauncher
{
    namespace AppModel = winrt::Windows::ApplicationModel;
    using std::filesystem::path;

    namespace
    {
        [[nodiscard]] path GetAppHostPath()
        {
            const auto installedLocation =
                AppModel::Package::Current().InstalledLocation();

            path packageRoot
            {
                installedLocation.Path().c_str()
            };

            const path nestedHostPath =
                packageRoot / L"FluentBitwarden.AppHost" / L"FluentBitwarden.AppHost.exe";

            return std::filesystem::exists(nestedHostPath)
                ? nestedHostPath
                : packageRoot / L"FluentBitwarden.AppHost.exe";
        }

        [[nodiscard]] constexpr std::wstring QuoteCommandLinePath(
            const path& path)
        {
            return L"\"" + path.native() + L"\"";
        }

        [[nodiscard]]
        std::vector<wchar_t> MakeCommandLineArgs(
            const path& exePath,
            std::wstring_view launchArguments)
        {
            std::wstring commandLine = QuoteCommandLinePath(exePath);

            if (!launchArguments.empty())
            {
                commandLine += L' ';
                commandLine += launchArguments;
            }

            // CreateProcessW may modify lpCommandLine, so pass a mutable,
            // null-terminated buffer instead of read-only memory.
            std::vector<wchar_t> buffer{
                commandLine.begin(),
                commandLine.end()
            };

            buffer.push_back(L'\0');

            return buffer;
        }

        void CreateAppHostProcess(
            const std::filesystem::path& appHostPath,
            std::vector<wchar_t>& commandLine)
        {
            STARTUPINFOW startupInfo{};
            startupInfo.cb = sizeof(startupInfo);

            PROCESS_INFORMATION processInfo{};

            THROW_LAST_ERROR_IF(
                !::CreateProcessW(
                appHostPath.c_str(),
                commandLine.data(),
                nullptr,
                nullptr,
                FALSE,
                0,
                nullptr,
                appHostPath.parent_path().c_str(),
                &startupInfo,
                &processInfo));

            wil::unique_handle processHandle{
                processInfo.hProcess
            };

            wil::unique_handle threadHandle{
                processInfo.hThread
            };
        }
    }

    void ActivateAppHost(std::wstring launchArguments)
    {
        const auto appHostPath = GetAppHostPath();
        auto commandLineArgs = MakeCommandLineArgs(appHostPath, launchArguments);

        CreateAppHostProcess(appHostPath, commandLineArgs);
    }
}

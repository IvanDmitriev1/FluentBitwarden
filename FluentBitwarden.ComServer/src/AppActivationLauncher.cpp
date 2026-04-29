#include "pch.h"
#include "AppActivationLauncher.h"

namespace FluentBitwarden::ComServer::AppActivationLauncher
{
    namespace AppModel = winrt::Windows::ApplicationModel;
    using std::filesystem::path;

    namespace
    {
        [[nodiscard]] path GetMainAppPath()
        {
            const auto installedLocation =
                AppModel::Package::Current().InstalledLocation();

            path packageRoot
            {
                installedLocation.Path().c_str()
            };

            return packageRoot / L"FluentBitwarden" / L"FluentBitwarden.exe";
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

        void CreateMainAppProcess(
            const std::filesystem::path& mainAppPath,
            std::vector<wchar_t>& commandLine)
        {
            STARTUPINFOW startupInfo{};
            startupInfo.cb = sizeof(startupInfo);

            PROCESS_INFORMATION processInfo{};

            THROW_LAST_ERROR_IF(
                !::CreateProcessW(
                mainAppPath.c_str(),
                commandLine.data(),
                nullptr,
                nullptr,
                FALSE,
                0,
                nullptr,
                mainAppPath.parent_path().c_str(),
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

    void ActivateMainApp(std::wstring launchArguments)
    {
        const auto mainAppPath = GetMainAppPath();
        auto commandLineArgs = MakeCommandLineArgs(mainAppPath, launchArguments);

        CreateMainAppProcess(mainAppPath, commandLineArgs);
    }
}

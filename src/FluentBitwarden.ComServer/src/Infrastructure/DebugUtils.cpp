#include "pch.h"
#include "DebugUtils.h"

namespace FluentBitwarden::ComServer::DebugUtils
{
    void AttachDebugger()
    {
#if defined(NDEBUG)
        return;
#elif defined(_DEBUG)
        if (IsDebuggerPresent())
        {
            return;
        }
#endif

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
}

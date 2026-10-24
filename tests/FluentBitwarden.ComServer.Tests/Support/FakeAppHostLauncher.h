#pragma once
#include "Infrastructure/AppHostLauncher/IAppHostLauncher.h"

#include <string>
#include <vector>

namespace FluentBitwarden::ComServer::Tests::Support
{
    class FakeAppHostLauncher final : public IAppHostLauncher
    {
    public:
        explicit FakeAppHostLauncher(std::vector<std::string>& calls)
            : m_calls(calls)
        {
        }

        void ActivateHeadless() const override
        {
            ++callCount;
            m_calls.emplace_back("launch");
            if (FAILED(failure))
            {
                THROW_HR(failure);
            }
        }

        HRESULT failure{ S_OK };
        mutable unsigned callCount{};

    private:
        std::vector<std::string>& m_calls;
    };
}

#pragma once
#include <pch.h>
#include "Infrastructure/AppHostLauncher/IAppHostLauncher.h"

namespace FluentBitwarden::ComServer
{
    class AppHostLauncher final : public IAppHostLauncher
    {
    public:
        void ActivateHeadless() const override;
    };
}

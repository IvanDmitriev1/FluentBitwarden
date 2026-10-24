#pragma once

namespace FluentBitwarden::ComServer
{
	struct IAppHostLauncher
	{
		virtual ~IAppHostLauncher() = default;
		virtual void ActivateHeadless() const = 0;
	};
}

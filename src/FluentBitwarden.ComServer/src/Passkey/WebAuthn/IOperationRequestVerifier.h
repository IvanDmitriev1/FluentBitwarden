#pragma once
#include "pch.h"

namespace FluentBitwarden::ComServer
{
	struct IOperationRequestVerifier
	{
		virtual ~IOperationRequestVerifier() = default;

		virtual void Verify(const WEBAUTHN_PLUGIN_OPERATION_REQUEST& request) const = 0;
	};
}

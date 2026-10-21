#pragma once
#include "Passkey/Ipc/IPasskeyClient.h"

#include <optional>
#include <string>
#include <vector>

namespace FluentBitwarden::ComServer::Tests::Support
{
    class FakePasskeyClient final : public IPasskeyClient
    {
    public:
        explicit FakePasskeyClient(std::vector<std::string>& calls)
            : m_calls(calls)
        {
        }

        wil::task<WebAuthn::PasskeyMakeCredentialResponse> MakeCredential(
            WebAuthn::PasskeyMakeCredentialRequest request) override
        {
            ++makeCredentialCallCount;
            lastMakeCredentialRequest = std::move(request);
            m_calls.emplace_back("make-credential");
            if (FAILED(makeCredentialFailure))
            {
                THROW_HR(makeCredentialFailure);
            }

            co_return makeCredentialResponse;
        }

        wil::task<WebAuthn::PasskeyAssertionResponse> GetAssertion(
            WebAuthn::PasskeyGetAssertionRequest request) override
        {
            ++getAssertionCallCount;
            lastGetAssertionRequest = std::move(request);
            m_calls.emplace_back("get-assertion");
            if (FAILED(getAssertionFailure))
            {
                THROW_HR(getAssertionFailure);
            }

            co_return getAssertionResponse;
        }

        HRESULT makeCredentialFailure{ S_OK };
        HRESULT getAssertionFailure{ S_OK };
        unsigned makeCredentialCallCount{};
        unsigned getAssertionCallCount{};
        std::optional<WebAuthn::PasskeyMakeCredentialRequest> lastMakeCredentialRequest;
        std::optional<WebAuthn::PasskeyGetAssertionRequest> lastGetAssertionRequest;
        WebAuthn::PasskeyMakeCredentialResponse makeCredentialResponse;
        WebAuthn::PasskeyAssertionResponse getAssertionResponse;

    private:
        std::vector<std::string>& m_calls;
    };
}

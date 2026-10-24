#include "pch.h"
#include "Passkey/PluginAuthenticator.h"
#include "../Support/FakeAppHostLauncher.h"
#include "../Support/FakePasskeyClient.h"

#include <array>
#include <string>
#include <vector>

using namespace Microsoft::VisualStudio::CppUnitTestFramework;
using namespace FluentBitwarden::ComServer;
using namespace FluentBitwarden::ComServer::Tests::Support;

namespace FluentBitwarden::ComServer::Tests
{
    namespace
    {
        struct PluginAuthenticatorHarness
        {
            std::vector<std::string> calls;
            FakePasskeyClient* passkeyClient{};
            FakeAppHostLauncher* appHostLauncher{};
            winrt::com_ptr<IPluginAuthenticator> authenticator;

            PluginAuthenticatorHarness()
            {
                auto clientDouble = std::make_unique<FakePasskeyClient>(calls);
                auto launcherDouble = std::make_unique<FakeAppHostLauncher>(calls);
                passkeyClient = clientDouble.get();
                appHostLauncher = launcherDouble.get();
                authenticator = winrt::make<PluginAuthenticator>(
                    std::move(clientDouble),
                    std::move(launcherDouble));
            }
        };

        struct NullPointerCase
        {
            bool request;
            bool response;
        };

        constexpr std::array NullPointerCases{
            NullPointerCase{ true, false },
            NullPointerCase{ false, true },
            NullPointerCase{ true, true }
        };
    }

    TEST_CLASS(PluginAuthenticatorTests)
    {
    public:
        TEST_METHOD(MakeCredentialRejectsEachNullPointerCombinationBeforeCallingCollaborators)
        {
            for (const auto& testCase : NullPointerCases)
            {
                PluginAuthenticatorHarness harness;
                WEBAUTHN_PLUGIN_OPERATION_REQUEST request{};
                WEBAUTHN_PLUGIN_OPERATION_RESPONSE response{};

                const HRESULT result = harness.authenticator->MakeCredential(
                    testCase.request ? nullptr : &request,
                    testCase.response ? nullptr : &response);

                Assert::AreEqual(E_POINTER, result);
                Assert::IsTrue(harness.calls.empty());
            }
        }

        TEST_METHOD(GetAssertionRejectsEachNullPointerCombinationBeforeCallingCollaborators)
        {
            for (const auto& testCase : NullPointerCases)
            {
                PluginAuthenticatorHarness harness;
                WEBAUTHN_PLUGIN_OPERATION_REQUEST request{};
                WEBAUTHN_PLUGIN_OPERATION_RESPONSE response{};

                const HRESULT result = harness.authenticator->GetAssertion(
                    testCase.request ? nullptr : &request,
                    testCase.response ? nullptr : &response);

                Assert::AreEqual(E_POINTER, result);
                Assert::IsTrue(harness.calls.empty());
            }
        }

        TEST_METHOD(InvalidRequestsStopBothCredentialFlowsBeforeResponseWritesOrClientCalls)
        {
            for (const bool getAssertion : { false, true })
            {
                PluginAuthenticatorHarness harness;
                std::array<BYTE, 2> encodedRequest{ 0x11, 0x12 };
                WEBAUTHN_PLUGIN_OPERATION_REQUEST request{};
                request.requestType = WEBAUTHN_PLUGIN_REQUEST_TYPE_CTAP2_CBOR;
                request.cbEncodedRequest = static_cast<DWORD>(encodedRequest.size());
                request.pbEncodedRequest = encodedRequest.data();
                std::array<BYTE, 1> previousResponse{ 0x31 };
                WEBAUTHN_PLUGIN_OPERATION_RESPONSE response{};
                response.cbEncodedResponse = static_cast<DWORD>(previousResponse.size());
                response.pbEncodedResponse = previousResponse.data();

                const HRESULT result = getAssertion
                    ? harness.authenticator->GetAssertion(&request, &response)
                    : harness.authenticator->MakeCredential(&request, &response);

                Assert::AreEqual(E_INVALIDARG, result);
                Assert::IsTrue(harness.calls.empty());
                Assert::AreEqual(0U, harness.passkeyClient->makeCredentialCallCount);
                Assert::AreEqual(0U, harness.passkeyClient->getAssertionCallCount);
                Assert::AreEqual(static_cast<DWORD>(previousResponse.size()), response.cbEncodedResponse);
                Assert::IsTrue(response.pbEncodedResponse == previousResponse.data());
            }
        }

        TEST_METHOD(CancelOperationRejectsNullAndAcceptsANonNullRequestWithoutCallingCollaborators)
        {
            PluginAuthenticatorHarness harness;
            WEBAUTHN_PLUGIN_CANCEL_OPERATION_REQUEST request{};

            Assert::AreEqual(E_POINTER, harness.authenticator->CancelOperation(nullptr));
            Assert::AreEqual(S_OK, harness.authenticator->CancelOperation(&request));
            Assert::IsTrue(harness.calls.empty());
        }

        TEST_METHOD(GetLockStatusHandlesNullSuccessAndLauncherFailure)
        {
            PluginAuthenticatorHarness nullHarness;
            Assert::AreEqual(E_POINTER, nullHarness.authenticator->GetLockStatus(nullptr));
            Assert::AreEqual(0U, nullHarness.appHostLauncher->callCount);
            Assert::IsTrue(nullHarness.calls.empty());

            PluginAuthenticatorHarness successHarness;
            PLUGIN_LOCK_STATUS status = PluginUnlocked;
            Assert::AreEqual(S_OK, successHarness.authenticator->GetLockStatus(&status));
            Assert::IsTrue(status == PluginLocked);
            Assert::AreEqual(1U, successHarness.appHostLauncher->callCount);
            Assert::IsTrue(successHarness.calls == std::vector<std::string>{ "launch" });

            PluginAuthenticatorHarness failureHarness;
            failureHarness.appHostLauncher->failure = HRESULT_FROM_WIN32(ERROR_ACCESS_DENIED);
            status = PluginUnlocked;
            Assert::AreEqual(
                HRESULT_FROM_WIN32(ERROR_ACCESS_DENIED),
                failureHarness.authenticator->GetLockStatus(&status));
            Assert::IsTrue(status == PluginUnlocked);
            Assert::AreEqual(1U, failureHarness.appHostLauncher->callCount);
            Assert::IsTrue(failureHarness.calls == std::vector<std::string>{ "launch" });
        }
    };
}

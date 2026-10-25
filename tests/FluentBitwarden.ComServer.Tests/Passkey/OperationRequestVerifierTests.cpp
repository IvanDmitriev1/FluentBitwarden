#include "pch.h"
#include "Passkey/WebAuthn/OperationRequestVerifier.h"

#include <array>

using namespace Microsoft::VisualStudio::CppUnitTestFramework;
using namespace FluentBitwarden::ComServer::WebAuthn;

namespace FluentBitwarden::ComServer::Tests
{
    TEST_CLASS(OperationRequestVerifierTests)
    {
    public:
        TEST_METHOD(InvalidRequestTypeAndMissingRequestOrSignatureDataFailBeforeRegistrationKeyLookup)
        {
            enum class InvalidField
            {
                RequestType,
                EncodedRequestLength,
                EncodedRequestPointer,
                SignatureLength,
                SignaturePointer
            };

            constexpr std::array cases{
                InvalidField::RequestType,
                InvalidField::EncodedRequestLength,
                InvalidField::EncodedRequestPointer,
                InvalidField::SignatureLength,
                InvalidField::SignaturePointer
            };
            std::array<BYTE, 2> encodedRequest{ 0x01, 0x02 };
            std::array<BYTE, 2> signature{ 0x03, 0x04 };

            for (const auto invalidField : cases)
            {
                WEBAUTHN_PLUGIN_OPERATION_REQUEST request{};
                request.requestType = WEBAUTHN_PLUGIN_REQUEST_TYPE_CTAP2_CBOR;
                request.cbEncodedRequest = static_cast<DWORD>(encodedRequest.size());
                request.pbEncodedRequest = encodedRequest.data();
                request.cbRequestSignature = static_cast<DWORD>(signature.size());
                request.pbRequestSignature = signature.data();
                switch (invalidField)
                {
                case InvalidField::RequestType:
                    request.requestType = static_cast<WEBAUTHN_PLUGIN_REQUEST_TYPE>(0);
                    break;
                case InvalidField::EncodedRequestLength:
                    request.cbEncodedRequest = 0;
                    break;
                case InvalidField::EncodedRequestPointer:
                    request.pbEncodedRequest = nullptr;
                    break;
                case InvalidField::SignatureLength:
                    request.cbRequestSignature = 0;
                    break;
                case InvalidField::SignaturePointer:
                    request.pbRequestSignature = nullptr;
                    break;
                }

                HRESULT result = S_OK;
                try
                {
                    OperationRequestVerifier::VerifyOperationRequest(request);
                }
                catch (...)
                {
                    result = wil::ResultFromCaughtException();
                }

                Assert::AreEqual(E_INVALIDARG, result);
            }
        }
    };
}

import { AutofillUi } from "./autofillUi";
import { detectCredentialFields, type DetectedCredentialFields } from "./fieldDetector";
import { fillCredentialFields } from "./fillFields";
import { CredentialScanCoordinator } from "./scanCoordinator";
import type {
  BrowserCredentialAvailabilityRequest,
  BrowserCredentialPart,
  BrowserCredentialListItem
} from "../shared/nativeProtocol";
import type {
  CredentialFieldsDetectedMessage,
  ExtensionMessage,
  ExtensionResponse,
  FillCredentialMessage
} from "../shared/extensionMessages";
import { PasswordFillParts, TotpFillParts } from "./credentialParts";
import {
  isBrowserCredentialAvailabilityResponse,
  isBrowserCredentialFillResponse,
  parseExtensionResponse
} from "./responseValidation";

const autofillUi = new AutofillUi();
const scanCoordinator = new CredentialScanCoordinator(handleCredentialFieldsChanged);

let fillInProgress = false;

type FillMode = BrowserCredentialPart;

scanCoordinator.start();
window.addEventListener("pagehide", dispose, { once: true });

function dispose(): void {
  scanCoordinator.dispose();
  autofillUi.dispose();
}

function handleCredentialFieldsChanged(
  fields: DetectedCredentialFields,
  currentVersion: number
): void {
  const fillMode = getFillMode(fields);

  if (fillMode === null) {
    autofillUi.hide();
    return;
  }

  const message: CredentialFieldsDetectedMessage = {
    type: "credentialFieldsDetected",
    payload: createAvailabilityRequest()
  };

  void sendRuntimeMessage(message, isBrowserCredentialAvailabilityResponse)
    .then((response) => {
      if (currentVersion !== scanCoordinator.currentVersion) {
        return;
      }

      if (!response.ok || response.payload.items.length === 0) {
        autofillUi.hide();
        return;
      }

      const latestFields = detectCredentialFields();
      if (
        getFillMode(latestFields) !== fillMode ||
        latestFields.signature !== scanCoordinator.currentSignature
      ) {
        autofillUi.hide();
        scanCoordinator.scheduleScan(0);
        return;
      }

      const credentialItem = response.payload.items[0];
      if (credentialItem) {
        const targetFields = fillMode === TotpFillParts
          ? latestFields.otpFields
          : latestFields.passwordFields;

        autofillUi.show(
          targetFields,
          credentialItem,
          (item) => requestCredentialFill(item, fillMode)
        );
      }
    })
    .catch(() => {
      autofillUi.hide();
    });
}

async function requestCredentialFill(
  item: BrowserCredentialListItem,
  fillMode: FillMode
): Promise<void> {
  if (fillInProgress) {
    return;
  }

  fillInProgress = true;

  const pageContext = getPageContext();
  const message: FillCredentialMessage = {
    type: "fillCredential",
    payload: {
      itemId: item.id,
      url: pageContext.url,
      part: fillMode
    }
  };

  try {
    const response = await sendRuntimeMessage(message, isBrowserCredentialFillResponse);
    if (response.ok) {
      fillCredentialFields(response.payload);
    }
  } finally {
    fillInProgress = false;
  }
}

function createAvailabilityRequest(): BrowserCredentialAvailabilityRequest {
  const pageContext = getPageContext();

  return {
    url: pageContext.url
  };
}

function getFillMode(fields: DetectedCredentialFields): FillMode | null {
  if (fields.hasPasswordField) {
    return PasswordFillParts;
  }

  if (fields.hasOtpField) {
    return TotpFillParts;
  }

  return null;
}

interface PageContext {
  url: string;
}

function getPageContext(): PageContext {
  return {
    url: window.location.href
  };
}

function sendRuntimeMessage<TPayload>(
  message: ExtensionMessage,
  validatePayload: (value: unknown) => value is TPayload
): Promise<ExtensionResponse<TPayload>> {
  return new Promise((resolve) => {
    chrome.runtime.sendMessage(message, (response: unknown) => {
      const runtimeError = chrome.runtime.lastError;
      if (runtimeError) {
        resolve({
          ok: false,
          error: {
            code: "runtime_error",
            message: runtimeError.message ?? "Runtime message failed."
          }
        });
        return;
      }

      resolve(parseExtensionResponse(response, validatePayload));
    });
  });
}

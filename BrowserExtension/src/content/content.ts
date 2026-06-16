import { AutofillUi } from "./autofillUi";
import { detectCredentialFields, type DetectedCredentialFields } from "./fieldDetector";
import { fillCredentialFields } from "./fillFields";
import type {
  BrowserCredentialAvailabilityRequest,
  BrowserCredentialAvailabilityResponse,
  BrowserCredentialPart,
  BrowserCredentialFillResponse,
  BrowserCredentialListItem
} from "../shared/nativeProtocol";
import type {
  CredentialFieldsDetectedMessage,
  ExtensionMessage,
  ExtensionResponse,
  FillCredentialMessage
} from "../shared/extensionMessages";

const ScanDebounceMs = 250;
const ObservedAttributes = [
  "type",
  "name",
  "id",
  "autocomplete",
  "placeholder",
  "class",
  "style"
];

const PasswordFillPart = 3 satisfies BrowserCredentialPart;
const TotpFillPart = 4 satisfies BrowserCredentialPart;

const autofillUi = new AutofillUi();

let lastDetectionSignature: string | null = null;
let requestVersion = 0;
let scanTimer: ReturnType<typeof setTimeout> | null = null;
let fillInProgress = false;

type FillMode = BrowserCredentialPart;

scanAndReport();

const observer = new MutationObserver(() => {
  scheduleScan();
});

observer.observe(document.documentElement, {
  childList: true,
  subtree: true,
  attributes: true,
  attributeFilter: ObservedAttributes
});

function scheduleScan(delayMs = ScanDebounceMs): void {
  if (scanTimer) {
    clearTimeout(scanTimer);
  }

  scanTimer = setTimeout(() => {
    scanTimer = null;
    scanAndReport();
  }, delayMs);
}

function scanAndReport(): void {
  const fields = detectCredentialFields();
  const fillMode = getFillMode(fields);

  if (fillMode === null) {
    lastDetectionSignature = fields.signature;
    requestVersion += 1;
    autofillUi.hide();
    return;
  }

  if (fields.signature === lastDetectionSignature) {
    return;
  }

  lastDetectionSignature = fields.signature;
  const currentRequestVersion = requestVersion + 1;
  requestVersion = currentRequestVersion;

  const message: CredentialFieldsDetectedMessage = {
    type: "credentialFieldsDetected",
    payload: createAvailabilityRequest()
  };

  void sendRuntimeMessage<BrowserCredentialAvailabilityResponse>(message)
    .then((response) => {
      if (currentRequestVersion !== requestVersion) {
        return;
      }

      if (!response.ok || response.payload.items.length === 0) {
        autofillUi.hide();
        return;
      }

      const latestFields = detectCredentialFields();
      if (getFillMode(latestFields) !== fillMode || latestFields.signature !== lastDetectionSignature) {
        autofillUi.hide();
        scheduleScan(0);
        return;
      }

      const credentialItem = response.payload.items[0];
      if (credentialItem) {
        const targetFields = fillMode === TotpFillPart
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
    const response = await sendRuntimeMessage<BrowserCredentialFillResponse>(message);
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
    return PasswordFillPart;
  }

  if (fields.hasOtpField) {
    return TotpFillPart;
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
  message: ExtensionMessage
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

      resolve(parseExtensionResponse<TPayload>(response));
    });
  });
}

function parseExtensionResponse<TPayload>(response: unknown): ExtensionResponse<TPayload> {
  if (isRecord(response) && response.ok === true && "payload" in response) {
    return {
      ok: true,
      payload: response.payload as TPayload
    };
  }

  if (
    isRecord(response) &&
    response.ok === false &&
    isRecord(response.error) &&
    typeof response.error.code === "string" &&
    typeof response.error.message === "string"
  ) {
    return {
      ok: false,
      error: {
        code: response.error.code,
        message: response.error.message
      }
    };
  }

  return {
    ok: false,
    error: {
      code: "invalid_response",
      message: "Background returned an invalid response."
    }
  };
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

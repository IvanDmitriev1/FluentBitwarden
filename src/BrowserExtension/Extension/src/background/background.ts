import { AvailabilityCache } from "./availabilityCache";
import { NativeClient, NativeClientError } from "./nativeClient";
import {
  NativeRequests,
  type BrowserCredentialAvailabilityRequest,
  type BrowserCredentialAvailabilityResponse,
  type BrowserCredentialFillRequest,
  type BrowserCredentialFillResponse
} from "../shared/nativeProtocol";
import {
  type CredentialFieldsDetectedMessage,
  type ExtensionResponse,
  type FillCredentialMessage,
  isCredentialFieldsDetectedMessage,
  isExtensionMessage,
  isFillCredentialMessage
} from "../shared/extensionMessages";

const AvailabilityCacheTtlMs = 60_000;

const nativeClient = new NativeClient();
const availabilityCache = new AvailabilityCache(AvailabilityCacheTtlMs);
let lastVaultUnlocked: boolean | null = null;

chrome.runtime.onMessage.addListener((message: unknown, sender, sendResponse) => {
  handleMessage(message, sender)
    .then(sendResponse)
    .catch(() => {
      sendResponse(errorResponse("background_error", "Background request failed."));
    });

  return true;
});

async function handleMessage(
  message: unknown,
  sender: chrome.runtime.MessageSender
): Promise<ExtensionResponse<unknown>> {
  if (!isExtensionMessage(message)) {
    return errorResponse("invalid_message", "Unsupported extension message.");
  }

  if (isCredentialFieldsDetectedMessage(message)) {
    return handleCredentialFieldsDetected(message, sender);
  }

  if (isFillCredentialMessage(message)) {
    return handleFillCredential(message, sender);
  }

  return errorResponse("invalid_message", "Unsupported extension message.");
}

async function handleCredentialFieldsDetected(
  message: CredentialFieldsDetectedMessage,
  sender: chrome.runtime.MessageSender
): Promise<ExtensionResponse<BrowserCredentialAvailabilityResponse>> {
  const senderInfo = validateSender(message.payload.url, sender);
  if (!senderInfo.ok) {
    return senderInfo;
  }

  const cacheKey = createAvailabilityCacheKey(senderInfo.payload.origin);
  const vaultStatus = await getVaultStatus();
  if (!vaultStatus.ok) {
    return vaultStatus;
  }

  if (!vaultStatus.payload.isVaultUnlocked) {
    availabilityCache.clear();
    return successResponse({ items: [] });
  }

  const cached = availabilityCache.get(cacheKey);
  if (cached) {
    return successResponse(cached);
  }

  const payload: BrowserCredentialAvailabilityRequest = {
    ...message.payload,
    url: senderInfo.payload.url
  };

  try {
    const response = await nativeClient.send(
      NativeRequests.GetCredentialAvailability,
      payload
    );

    if (response.items.length > 0) {
      availabilityCache.set(cacheKey, response);
    }

    return successResponse(response);
  } catch (error) {
    return nativeErrorResponse(error);
  }
}

async function handleFillCredential(
  message: FillCredentialMessage,
  sender: chrome.runtime.MessageSender
): Promise<ExtensionResponse<BrowserCredentialFillResponse>> {
  const senderInfo = validateSender(message.payload.url, sender);
  if (!senderInfo.ok) {
    return senderInfo;
  }

  const payload: BrowserCredentialFillRequest = {
    ...message.payload,
    url: senderInfo.payload.url
  };

  try {
    const response = await nativeClient.send(
      NativeRequests.GetCredentialFill,
      payload
    );

    return successResponse(response);
  } catch (error) {
    return nativeErrorResponse(error);
  }
}

function createAvailabilityCacheKey(origin: string): string {
  return origin;
}

async function getVaultStatus(): Promise<ExtensionResponse<{ isVaultUnlocked: boolean }>> {
  try {
    const status = await nativeClient.send(
      NativeRequests.GetVaultStatus,
      {}
    );

    if (lastVaultUnlocked !== null && lastVaultUnlocked !== status.isVaultUnlocked) {
      availabilityCache.clear();
    }

    lastVaultUnlocked = status.isVaultUnlocked;

    return successResponse({
      isVaultUnlocked: status.isVaultUnlocked
    });
  } catch (error) {
    return nativeErrorResponse(error);
  }
}

interface ValidatedSender {
  url: string;
  origin: string;
}

function validateSender(
  payloadUrl: string,
  sender: chrome.runtime.MessageSender
): ExtensionResponse<ValidatedSender> {
  if (!sender.url) {
    return errorResponse("sender_url_missing", "Sender URL is missing.");
  }

  let senderUrl: URL;
  try {
    senderUrl = new URL(sender.url);
  } catch {
    return errorResponse("sender_url_invalid", "Sender URL is invalid.");
  }

  let requestedUrl: URL;
  try {
    requestedUrl = new URL(payloadUrl);
  } catch {
    return errorResponse("payload_url_invalid", "Payload URL is invalid.");
  }

  if (requestedUrl.origin !== senderUrl.origin) {
    return errorResponse("origin_mismatch", "Payload URL origin does not match the sender origin.");
  }

  return successResponse({
    url: senderUrl.href,
    origin: senderUrl.origin
  });
}

function successResponse<TPayload>(payload: TPayload): ExtensionResponse<TPayload> {
  return {
    ok: true,
    payload
  };
}

function errorResponse(code: string, message: string): ExtensionResponse<never> {
  return {
    ok: false,
    error: {
      code,
      message
    }
  };
}

function nativeErrorResponse(error: unknown): ExtensionResponse<never> {
  if (error instanceof NativeClientError) {
    return errorResponse(error.code, error.message);
  }

  return errorResponse("native_request_failed", "Native host request failed.");
}

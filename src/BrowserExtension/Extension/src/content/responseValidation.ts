import type {
  BrowserCredentialAvailabilityResponse,
  BrowserCredentialFillResponse,
  BrowserCredentialListItem,
  BrowserCredentialPart
} from "../shared/nativeProtocol";
import type { ExtensionResponse } from "../shared/extensionMessages";

type Validator<T> = (value: unknown) => value is T;

export function parseExtensionResponse<TPayload>(
  response: unknown,
  validatePayload: Validator<TPayload>
): ExtensionResponse<TPayload> {
  if (!isRecord(response) || typeof response.ok !== "boolean") {
    return invalidExtensionResponse();
  }

  if (response.ok) {
    if (!validatePayload(response.payload)) {
      return invalidExtensionResponse();
    }

    return {
      ok: true,
      payload: response.payload
    };
  }

  if (!isRecord(response.error) || !isString(response.error.code) || !isString(response.error.message)) {
    return invalidExtensionResponse();
  }

  return {
    ok: false,
    error: {
      code: response.error.code,
      message: response.error.message
    }
  };
}

export function isBrowserCredentialAvailabilityResponse(
  value: unknown
): value is BrowserCredentialAvailabilityResponse {
  return isRecord(value) && Array.isArray(value.items) && value.items.every(isBrowserCredentialListItem);
}

export function isBrowserCredentialFillResponse(value: unknown): value is BrowserCredentialFillResponse {
  return (
    isRecord(value) &&
    isBrowserCredentialPart(value.returnedParts) &&
    isOptionalString(value.username) &&
    isOptionalString(value.password) &&
    isOptionalString(value.totp) &&
    isOptionalString(value.totpExpiresAt)
  );
}

function isBrowserCredentialListItem(value: unknown): value is BrowserCredentialListItem {
  return isRecord(value) && isString(value.id) && isString(value.username);
}

function isBrowserCredentialPart(value: unknown): value is BrowserCredentialPart {
  return typeof value === "number" && Number.isInteger(value) && value >= 0 && value <= 7;
}

function invalidExtensionResponse(): ExtensionResponse<never> {
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

function isString(value: unknown): value is string {
  return typeof value === "string";
}

function isOptionalString(value: unknown): value is string | null | undefined {
  return value === undefined || value === null || isString(value);
}

import { ExtensionMessageTypes } from "./browserMessageTypes";
import type {
  BrowserCredentialAvailabilityRequest,
  BrowserCredentialAvailabilityResponse,
  BrowserCredentialFillRequest,
  BrowserCredentialFillResponse
} from "./nativeProtocol";
import {
  isBrowserCredentialPart,
  isRecord,
  isString,
  type Validator
} from "./nativeProtocol";

export interface ExtensionError {
  code: string;
  message: string;
}

export type ExtensionResponse<TPayload> =
  | {
      ok: true;
      payload: TPayload;
    }
  | {
      ok: false;
      error: ExtensionError;
    };

export interface CredentialFieldsDetectedMessage {
  type: typeof ExtensionMessageTypes.CredentialFieldsDetected;
  payload: BrowserCredentialAvailabilityRequest;
}

export type FillCredentialPayload = BrowserCredentialFillRequest;

export interface FillCredentialMessage {
  type: typeof ExtensionMessageTypes.FillCredential;
  payload: FillCredentialPayload;
}

export type ExtensionMessage =
  | CredentialFieldsDetectedMessage
  | FillCredentialMessage;

export type CredentialFieldsDetectedResponse =
  ExtensionResponse<BrowserCredentialAvailabilityResponse>;

export type FillCredentialResponse =
  ExtensionResponse<BrowserCredentialFillResponse>;

export function isExtensionMessage(value: unknown): value is ExtensionMessage {
  return isCredentialFieldsDetectedMessage(value) || isFillCredentialMessage(value);
}

export function isCredentialFieldsDetectedMessage(
  value: unknown
): value is CredentialFieldsDetectedMessage {
  if (!isRecord(value) || value.type !== ExtensionMessageTypes.CredentialFieldsDetected) {
    return false;
  }

  const payload = value.payload;
  return (
    isRecord(payload) &&
    isString(payload.url)
  );
}

export function isFillCredentialMessage(value: unknown): value is FillCredentialMessage {
  if (!isRecord(value) || value.type !== ExtensionMessageTypes.FillCredential) {
    return false;
  }

  const payload = value.payload;
  return (
    isRecord(payload) &&
    isString(payload.itemId) &&
    isString(payload.url) &&
    isBrowserCredentialPart(payload.part)
  );
}

export function parseExtensionResponse<TPayload>(
  response: unknown,
  validatePayload: Validator<TPayload>
): ExtensionResponse<TPayload> {
  if (
    isRecord(response) &&
    response.ok === true &&
    "payload" in response &&
    validatePayload(response.payload)
  ) {
    return {
      ok: true,
      payload: response.payload
    };
  }

  if (
    isRecord(response) &&
    response.ok === false &&
    isRecord(response.error) &&
    isString(response.error.code) &&
    isString(response.error.message)
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

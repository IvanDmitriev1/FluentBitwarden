import { ExtensionMessageTypes } from "./browserMessageTypes";
import type {
  BrowserCredentialAvailabilityRequest,
  BrowserCredentialAvailabilityResponse,
  BrowserCredentialFillRequest,
  BrowserCredentialFillResponse
} from "./nativeProtocol";
import { BrowserCredentialParts } from "./nativeProtocol";

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
    isCredentialPart(payload.part)
  );
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function isString(value: unknown): value is string {
  return typeof value === "string";
}

function isCredentialPart(value: unknown): value is number {
  const validPartMask =
    BrowserCredentialParts.Username |
    BrowserCredentialParts.Password |
    BrowserCredentialParts.Totp;

  return (
    typeof value === "number" &&
    Number.isInteger(value) &&
    value >= BrowserCredentialParts.None &&
    (value & ~validPartMask) === 0
  );
}

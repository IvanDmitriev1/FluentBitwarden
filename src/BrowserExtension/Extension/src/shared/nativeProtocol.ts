import type { BrowserNativeMessageType } from "./browserMessageTypes";
import { BrowserNativeMessageTypes } from "./browserMessageTypes";

export const NativeMessagingHostName = "com.fluentbitwarden.browseproxy";
export const NativeProtocolVersion = 1;

export type Validator<T> = (value: unknown) => value is T;

export interface NativeRequestEnvelope<TPayload> {
  version: typeof NativeProtocolVersion;
  requestId: string;
  type: BrowserNativeMessageType;
  payload: TPayload;
}

export interface NativeResponseEnvelope<TPayload> {
  requestId: string;
  payload: TPayload;
}

export interface BrowserVaultStatusRequest {
}

export interface BrowserVaultStatusResponse {
  isRunning: boolean;
  isVaultUnlocked: boolean;
}

export interface BrowserCredentialAvailabilityRequest {
  url: string;
}

export interface BrowserCredentialAvailabilityResponse {
  items: BrowserCredentialListItem[];
}

export interface BrowserCredentialListItem {
  id: string;
  username: string;
}

export interface BrowserCredentialFillRequest {
  itemId: string;
  url: string;
  part: BrowserCredentialPart;
}

export interface BrowserCredentialFillResponse {
  returnedParts: BrowserCredentialPart;
  username?: string | null;
  password?: string | null;
  totp?: string | null;
  totpExpiresAt?: string | null;
}

export const BrowserCredentialParts = {
  None: 0,
  Username: 1,
  Password: 2,
  Totp: 4
} as const;

export type BrowserCredentialPart = 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7;

export const PasswordFillParts = 3 satisfies BrowserCredentialPart;

export const TotpFillParts = BrowserCredentialParts.Totp satisfies BrowserCredentialPart;

export interface NativeRequestDescriptor<TPayload, TResponse> {
  type: BrowserNativeMessageType;
  validateResponse: Validator<TResponse>;
}

export const NativeRequests = {
  GetVaultStatus: {
    type: BrowserNativeMessageTypes.GetVaultStatus,
    validateResponse: isBrowserVaultStatusResponse
  },
  GetCredentialAvailability: {
    type: BrowserNativeMessageTypes.GetCredentialAvailability,
    validateResponse: isBrowserCredentialAvailabilityResponse
  },
  GetCredentialFill: {
    type: BrowserNativeMessageTypes.GetCredentialFill,
    validateResponse: isBrowserCredentialFillResponse
  }
} as const satisfies {
  GetVaultStatus: NativeRequestDescriptor<BrowserVaultStatusRequest, BrowserVaultStatusResponse>;
  GetCredentialAvailability: NativeRequestDescriptor<
    BrowserCredentialAvailabilityRequest,
    BrowserCredentialAvailabilityResponse
  >;
  GetCredentialFill: NativeRequestDescriptor<
    BrowserCredentialFillRequest,
    BrowserCredentialFillResponse
  >;
};

export function hasCredentialPart(
  value: BrowserCredentialPart,
  part: BrowserCredentialPart
): boolean {
  return (value & part) === part;
}

export function isBrowserVaultStatusResponse(
  value: unknown
): value is BrowserVaultStatusResponse {
  return (
    isRecord(value) &&
    isBoolean(value.isRunning) &&
    isBoolean(value.isVaultUnlocked)
  );
}

export function isBrowserCredentialAvailabilityResponse(
  value: unknown
): value is BrowserCredentialAvailabilityResponse {
  return (
    isRecord(value) &&
    Array.isArray(value.items) &&
    value.items.every(isBrowserCredentialListItem)
  );
}

export function isBrowserCredentialFillResponse(
  value: unknown
): value is BrowserCredentialFillResponse {
  return (
    isRecord(value) &&
    isBrowserCredentialPart(value.returnedParts) &&
    isOptionalString(value.username) &&
    isOptionalString(value.password) &&
    isOptionalString(value.totp) &&
    isOptionalString(value.totpExpiresAt)
  );
}

export function isBrowserCredentialListItem(
  value: unknown
): value is BrowserCredentialListItem {
  return (
    isRecord(value) &&
    isString(value.id) &&
    isString(value.username)
  );
}

export function isBrowserCredentialPart(value: unknown): value is BrowserCredentialPart {
  return (
    typeof value === "number" &&
    Number.isInteger(value) &&
    value >= BrowserCredentialParts.None &&
    value <= (
      BrowserCredentialParts.Username |
      BrowserCredentialParts.Password |
      BrowserCredentialParts.Totp
    )
  );
}

export function isNativeResponseEnvelope<TPayload>(
  value: unknown,
  validatePayload: Validator<TPayload>
): value is NativeResponseEnvelope<TPayload> {
  return (
    isRecord(value) &&
    isString(value.requestId) &&
    "payload" in value &&
    validatePayload(value.payload)
  );
}

export function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

export function isString(value: unknown): value is string {
  return typeof value === "string";
}

export function isBoolean(value: unknown): value is boolean {
  return typeof value === "boolean";
}

function isOptionalString(value: unknown): value is string | null | undefined {
  return value === undefined || value === null || isString(value);
}

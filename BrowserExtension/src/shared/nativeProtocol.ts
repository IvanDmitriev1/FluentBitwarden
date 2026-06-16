import type { BrowserNativeMessageType } from "./browserMessageTypes";

export const NativeMessagingHostName = "com.fluentbitwarden.browserhost";
export const NativeProtocolVersion = 1;

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

export type BrowserCredentialPart = number;

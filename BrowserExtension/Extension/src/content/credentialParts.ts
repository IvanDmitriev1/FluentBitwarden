import type { BrowserCredentialPart } from "../shared/nativeProtocol";

export const CredentialParts = {
  Username: 1,
  Password: 2,
  Totp: 4
} as const;

export const PasswordFillParts = 3 satisfies BrowserCredentialPart;
export const TotpFillParts = CredentialParts.Totp satisfies BrowserCredentialPart;

export function hasCredentialPart(value: BrowserCredentialPart, flag: BrowserCredentialPart): boolean {
  return (value & flag) === flag;
}

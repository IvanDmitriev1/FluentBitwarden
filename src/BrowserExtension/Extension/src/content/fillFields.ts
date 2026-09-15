import { detectCredentialFields } from "./fieldDetector";
import type { BrowserCredentialFillResponse } from "../shared/nativeProtocol";
import { CredentialParts, hasCredentialPart } from "./credentialParts";

export function fillCredentialFields(credential: BrowserCredentialFillResponse): void {
  const fields = detectCredentialFields();
  const otpField = fields.otpFields[0] ?? null;

  if (
    hasCredentialPart(credential.returnedParts, CredentialParts.Totp) &&
    hasValue(credential.totp) &&
    otpField
  ) {
    setInputValue(otpField, credential.totp);
    return;
  }

  const usernameField = fields.usernameFields[0] ?? null;
  const passwordField = fields.passwordFields[0] ?? null;

  if (
    hasCredentialPart(credential.returnedParts, CredentialParts.Username) &&
    hasValue(credential.username) &&
    usernameField
  ) {
    setInputValue(usernameField, credential.username);
  }

  if (
    hasCredentialPart(credential.returnedParts, CredentialParts.Password) &&
    hasValue(credential.password) &&
    passwordField
  ) {
    setInputValue(passwordField, credential.password);
  }
}

function hasValue(value: string | null | undefined): value is string {
  return value !== undefined && value !== null && value.length > 0;
}

function setInputValue(input: HTMLInputElement, value: string): void {
  const valueSetter = Object.getOwnPropertyDescriptor(
    HTMLInputElement.prototype,
    "value"
  )?.set;

  if (valueSetter) {
    valueSetter.call(input, value);
  } else {
    input.value = value;
  }

  input.dispatchEvent(new Event("input", { bubbles: true }));
  input.dispatchEvent(new Event("change", { bubbles: true }));
}

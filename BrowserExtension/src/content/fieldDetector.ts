export interface DetectedCredentialFields {
  passwordFields: HTMLInputElement[];
  usernameFields: HTMLInputElement[];
  otpFields: HTMLInputElement[];
  hasPasswordField: boolean;
  hasUsernameField: boolean;
  hasOtpField: boolean;
  signature: string;
}

const UsernameAttributePattern =
  /\b(user(name)?|email|e-mail|login|account|phone|mobile|tel)\b/i;
const NonUsernameAttributePattern =
  /\b(search|captcha|otp|totp|code|token|password|passwd|pass)\b/i;
const OtpAttributePattern =
  /\b(otp|totp|2fa|mfa|authenticator|verification|one[-_\s]?time|code)\b/i;

export function detectCredentialFields(root: ParentNode = document): DetectedCredentialFields {
  const inputs = Array.from(root.querySelectorAll("input")).filter(isVisibleFillableInput);
  const otpFields = inputs.filter(isOtpInput);
  const otpFieldSet = new Set(otpFields);
  const passwordFields = inputs.filter((input) => isPasswordInput(input) && !otpFieldSet.has(input));
  const usernameFields = uniqueInputs([
    ...inputs.filter(isUsernameAttributeCandidate),
    ...findUsernameFieldsNearPasswords(inputs, passwordFields)
  ]);

  return {
    passwordFields,
    usernameFields,
    otpFields,
    hasPasswordField: passwordFields.length > 0,
    hasUsernameField: usernameFields.length > 0,
    hasOtpField: otpFields.length > 0,
    signature: buildDetectionSignature(inputs, passwordFields, usernameFields, otpFields)
  };
}

function isVisibleFillableInput(input: HTMLInputElement): boolean {
  if (input.disabled || input.readOnly) {
    return false;
  }

  const type = input.type.toLowerCase();
  if (type === "hidden" || type === "button" || type === "submit" || type === "reset") {
    return false;
  }

  const rect = input.getBoundingClientRect();
  if (rect.width < 1 || rect.height < 1 || input.getClientRects().length === 0) {
    return false;
  }

  const style = window.getComputedStyle(input);
  return style.display !== "none" && style.visibility !== "hidden" && style.opacity !== "0";
}

function isPasswordInput(input: HTMLInputElement): boolean {
  return input.type.toLowerCase() === "password";
}

function isUsernameAttributeCandidate(input: HTMLInputElement): boolean {
  if (!isUsernameType(input)) {
    return false;
  }

  const autocomplete = input.getAttribute("autocomplete")?.toLowerCase() ?? "";
  if (autocomplete.includes("username") || autocomplete.includes("email") || autocomplete.includes("tel")) {
    return true;
  }

  const attributeText = getInputAttributeText(input);
  return UsernameAttributePattern.test(attributeText) && !NonUsernameAttributePattern.test(attributeText);
}

function isOtpInput(input: HTMLInputElement): boolean {
  const autocomplete = input.getAttribute("autocomplete")?.toLowerCase() ?? "";
  if (autocomplete === "one-time-code" || autocomplete.includes("one-time-code")) {
    return true;
  }

  if (!isOtpType(input)) {
    return false;
  }

  const attributes = getInputAttributeText(input, {
    includeInputMode: true,
    includeLabels: true
  });

  return OtpAttributePattern.test(attributes);
}

interface AttributeTextOptions {
  includeInputMode?: boolean;
  includeLabels?: boolean;
}

function getInputAttributeText(
  input: HTMLInputElement,
  options: AttributeTextOptions = {}
): string {
  const values = [
    input.name,
    input.id,
    input.placeholder,
    input.getAttribute("aria-label") ?? "",
    input.className
  ];

  if (options.includeInputMode) {
    values.push(input.getAttribute("inputmode") ?? "");
  }

  if (options.includeLabels) {
    values.push(getAssociatedLabelText(input), getNearbyLabelText(input));
  }

  return values.join(" ");
}

function getAssociatedLabelText(input: HTMLInputElement): string {
  const labelTexts: string[] = [];

  for (const label of Array.from(input.labels ?? [])) {
    labelTexts.push(label.textContent ?? "");
  }

  const labelledBy = input.getAttribute("aria-labelledby");
  if (labelledBy) {
    for (const id of labelledBy.split(/\s+/)) {
      const label = document.getElementById(id);
      if (label) {
        labelTexts.push(label.textContent ?? "");
      }
    }
  }

  return labelTexts.join(" ");
}

function getNearbyLabelText(input: HTMLInputElement): string {
  const labelTexts: string[] = [];
  let parent = input.parentElement;
  let depth = 0;

  while (parent && parent !== document.body && depth < 4) {
    for (const label of Array.from(parent.querySelectorAll("label"))) {
      labelTexts.push(label.textContent ?? "");
    }

    if (labelTexts.length > 0) {
      break;
    }

    parent = parent.parentElement;
    depth += 1;
  }

  const previous = input.previousElementSibling;
  if (previous?.tagName.toLowerCase() === "label") {
    labelTexts.push(previous.textContent ?? "");
  }

  const next = input.nextElementSibling;
  if (next?.tagName.toLowerCase() === "label") {
    labelTexts.push(next.textContent ?? "");
  }

  return labelTexts.join(" ");
}

function findUsernameFieldsNearPasswords(
  inputs: HTMLInputElement[],
  passwordFields: HTMLInputElement[]
): HTMLInputElement[] {
  const candidates: HTMLInputElement[] = [];

  for (const passwordField of passwordFields) {
    const scopeInputs = passwordField.form
      ? inputs.filter((input) => input.form === passwordField.form)
      : inputs;

    const passwordIndex = scopeInputs.indexOf(passwordField);
    if (passwordIndex <= 0) {
      continue;
    }

    for (let index = passwordIndex - 1; index >= 0; index -= 1) {
      const candidate = scopeInputs[index];
      if (candidate && isUsernameType(candidate)) {
        candidates.push(candidate);
        break;
      }
    }
  }

  return candidates;
}

function isUsernameType(input: HTMLInputElement): boolean {
  const type = input.type.toLowerCase();
  return type === "text" || type === "email" || type === "tel";
}

function isOtpType(input: HTMLInputElement): boolean {
  const type = input.type.toLowerCase();
  return type === "text" || type === "tel" || type === "number" || type === "password";
}

function uniqueInputs(inputs: HTMLInputElement[]): HTMLInputElement[] {
  return Array.from(new Set(inputs));
}

function buildDetectionSignature(
  inputs: HTMLInputElement[],
  passwordFields: HTMLInputElement[],
  usernameFields: HTMLInputElement[],
  otpFields: HTMLInputElement[]
): string {
  return [
    fieldGroupSignature("p", passwordFields, inputs),
    fieldGroupSignature("u", usernameFields, inputs),
    fieldGroupSignature("o", otpFields, inputs)
  ].join("|");
}

function fieldGroupSignature(
  prefix: string,
  fields: HTMLInputElement[],
  inputs: HTMLInputElement[]
): string {
  const signature = fields
    .map((input) => inputSignature(input, inputs))
    .join(",");

  return `${prefix}:${fields.length}:${signature}`;
}

function inputSignature(input: HTMLInputElement, inputs: HTMLInputElement[]): string {
  return [
    inputs.indexOf(input),
    input.type,
    input.name,
    input.id,
    input.getAttribute("autocomplete") ?? "",
    input.placeholder
  ].join(":");
}

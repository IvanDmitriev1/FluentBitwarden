export const BrowserNativeMessageTypes = {
  GetVaultStatus: 500,
  GetCredentialAvailability: 501,
  GetCredentialFill: 502
} as const;

export type BrowserNativeMessageType =
  (typeof BrowserNativeMessageTypes)[keyof typeof BrowserNativeMessageTypes];

export const ExtensionMessageTypes = {
  CredentialFieldsDetected: "credentialFieldsDetected",
  FillCredential: "fillCredential"
} as const;

export type ExtensionMessageType =
  (typeof ExtensionMessageTypes)[keyof typeof ExtensionMessageTypes];

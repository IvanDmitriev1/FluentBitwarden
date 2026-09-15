import { detectCredentialFields, type DetectedCredentialFields } from "./fieldDetector";

const ScanDebounceMs = 250;
const ObservedAttributes = [
  "type",
  "name",
  "id",
  "autocomplete",
  "placeholder",
  "class",
  "style"
];

export type CredentialScanHandler = (
  fields: DetectedCredentialFields,
  version: number
) => void;

export class CredentialScanCoordinator {
  private readonly observer = new MutationObserver(() => {
    this.scheduleScan();
  });
  private scanTimer: ReturnType<typeof setTimeout> | null = null;
  private signature: string | null = null;
  private version = 0;

  public constructor(private readonly scanHandler: CredentialScanHandler) {
  }

  public get currentSignature(): string | null {
    return this.signature;
  }

  public get currentVersion(): number {
    return this.version;
  }

  public start(): void {
    this.scanAndNotify();
    this.observer.observe(document.documentElement, {
      childList: true,
      subtree: true,
      attributes: true,
      attributeFilter: ObservedAttributes
    });
  }

  public scheduleScan(delayMs = ScanDebounceMs): void {
    if (this.scanTimer) {
      clearTimeout(this.scanTimer);
    }

    this.scanTimer = setTimeout(() => {
      this.scanTimer = null;
      this.scanAndNotify();
    }, delayMs);
  }

  public dispose(): void {
    this.observer.disconnect();

    if (this.scanTimer) {
      clearTimeout(this.scanTimer);
      this.scanTimer = null;
    }
  }

  private scanAndNotify(): void {
    const fields = detectCredentialFields();
    if (fields.signature === this.signature) {
      return;
    }

    this.signature = fields.signature;
    this.version += 1;
    this.scanHandler(fields, this.version);
  }
}

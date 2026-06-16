import type { BrowserCredentialAvailabilityResponse } from "../shared/nativeProtocol";

interface AvailabilityCacheEntry {
  expiresAt: number;
  response: BrowserCredentialAvailabilityResponse;
}

export class AvailabilityCache {
  private readonly entries = new Map<string, AvailabilityCacheEntry>();

  public constructor(private readonly ttlMs: number) {
  }

  public get(key: string): BrowserCredentialAvailabilityResponse | undefined {
    const entry = this.entries.get(key);
    if (!entry) {
      return undefined;
    }

    if (entry.expiresAt <= Date.now()) {
      this.entries.delete(key);
      return undefined;
    }

    return entry.response;
  }

  public set(key: string, response: BrowserCredentialAvailabilityResponse): void {
    this.entries.set(key, {
      expiresAt: Date.now() + this.ttlMs,
      response
    });
  }

  public clear(): void {
    this.entries.clear();
  }
}

import {
  NativeMessagingHostName,
  NativeProtocolVersion,
  isNativeResponseEnvelope,
  isRecord,
  isString,
  type NativeRequestDescriptor,
  type NativeRequestEnvelope
} from "../shared/nativeProtocol";

const RequestTimeoutMs = 10_000;
const IdleDisconnectMs = 60_000;

interface PendingRequest {
  complete: (payload: unknown) => void;
  reject: (error: NativeClientError) => void;
  timeoutId: ReturnType<typeof setTimeout>;
}

export class NativeClientError extends Error {
  public constructor(
    public readonly code: string,
    message: string
  ) {
    super(message);
    this.name = "NativeClientError";
  }
}

export class NativeClient {
  private port: chrome.runtime.Port | null = null;
  private readonly pendingRequests = new Map<string, PendingRequest>();
  private idleDisconnectTimer: ReturnType<typeof setTimeout> | null = null;

  public send<TPayload, TResponse>(
    request: NativeRequestDescriptor<TPayload, TResponse>,
    payload: TPayload
  ): Promise<TResponse> {
    let port: chrome.runtime.Port;

    try {
      port = this.ensurePort();
    } catch (error) {
      return Promise.reject(
        new NativeClientError("native_connect_failed", getErrorMessage(error))
      );
    }

    this.clearIdleDisconnectTimer();

    const requestId = createRequestId();
    const envelope: NativeRequestEnvelope<TPayload> = {
      version: NativeProtocolVersion,
      requestId,
      type: request.type,
      payload
    };

    return new Promise<TResponse>((resolve, reject) => {
      const timeoutId = setTimeout(() => {
        this.pendingRequests.delete(requestId);
        this.scheduleIdleDisconnectIfNeeded();
        reject(new NativeClientError("native_timeout", "Native host request timed out."));
      }, RequestTimeoutMs);

      this.pendingRequests.set(requestId, {
        complete: (responsePayload: unknown) => {
          if (!request.validateResponse(responsePayload)) {
            reject(
              new NativeClientError(
                "invalid_native_payload",
                "Native host returned an invalid response payload."
              )
            );
            return;
          }

          resolve(responsePayload);
        },
        reject,
        timeoutId
      });

      try {
        port.postMessage(envelope);
      } catch (error) {
        clearTimeout(timeoutId);
        this.pendingRequests.delete(requestId);
        this.scheduleIdleDisconnectIfNeeded();
        reject(new NativeClientError("native_post_failed", getErrorMessage(error)));
      }
    });
  }

  private ensurePort(): chrome.runtime.Port {
    if (this.port) {
      return this.port;
    }

    const port = chrome.runtime.connectNative(NativeMessagingHostName);
    port.onMessage.addListener(this.handlePortMessage);
    port.onDisconnect.addListener(this.handlePortDisconnect);
    this.port = port;
    return port;
  }

  private readonly handlePortMessage = (message: unknown): void => {
    const response = isNativeResponseEnvelope(message, isUnknown) ? message : null;
    if (!response) {
      const requestId = readRequestId(message);
      if (requestId) {
        this.rejectPendingRequest(
          requestId,
          new NativeClientError("invalid_native_response", "Native host returned an invalid response.")
        );
      }

      return;
    }

    const pending = this.pendingRequests.get(response.requestId);
    if (!pending) {
      return;
    }

    clearTimeout(pending.timeoutId);
    this.pendingRequests.delete(response.requestId);

    pending.complete(response.payload);

    this.scheduleIdleDisconnectIfNeeded();
  };

  private readonly handlePortDisconnect = (): void => {
    this.port = null;
    this.clearIdleDisconnectTimer();

    const message =
      chrome.runtime.lastError?.message ?? "Native messaging port disconnected.";

    for (const [requestId, pending] of this.pendingRequests) {
      clearTimeout(pending.timeoutId);
      pending.reject(new NativeClientError("native_disconnected", message));
      this.pendingRequests.delete(requestId);
    }
  };

  private rejectPendingRequest(requestId: string, error: NativeClientError): void {
    const pending = this.pendingRequests.get(requestId);
    if (!pending) {
      return;
    }

    clearTimeout(pending.timeoutId);
    this.pendingRequests.delete(requestId);
    pending.reject(error);
    this.scheduleIdleDisconnectIfNeeded();
  }

  private scheduleIdleDisconnectIfNeeded(): void {
    if (this.pendingRequests.size > 0 || !this.port || this.idleDisconnectTimer) {
      return;
    }

    this.idleDisconnectTimer = setTimeout(() => {
      this.idleDisconnectTimer = null;

      if (this.pendingRequests.size > 0 || !this.port) {
        return;
      }

      const port = this.port;
      this.port = null;
      port.disconnect();
    }, IdleDisconnectMs);
  }

  private clearIdleDisconnectTimer(): void {
    if (!this.idleDisconnectTimer) {
      return;
    }

    clearTimeout(this.idleDisconnectTimer);
    this.idleDisconnectTimer = null;
  }
}

function readRequestId(value: unknown): string | null {
  return isRecord(value) && isString(value.requestId) ? value.requestId : null;
}

function createRequestId(): string {
  return crypto.randomUUID();
}

function getErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : "Native messaging request failed.";
}

function isUnknown(value: unknown): value is unknown {
  return true;
}

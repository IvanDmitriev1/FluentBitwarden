import type { BrowserCredentialListItem } from "../shared/nativeProtocol";

const ButtonSize = 24;
const ButtonGap = 4;

interface ButtonEntry {
  button: HTMLButtonElement;
  targetField: HTMLInputElement;
}

export class AutofillUi {
  private readonly buttons: ButtonEntry[] = [];
  private fillHandler: ((item: BrowserCredentialListItem) => Promise<void>) | null = null;
  private credentialItem: BrowserCredentialListItem | null = null;

  public constructor() {
    window.addEventListener("scroll", this.repositionButtons, true);
    window.addEventListener("resize", this.repositionButtons);
  }

  public show(
    targetFields: HTMLInputElement[],
    credentialItem: BrowserCredentialListItem,
    fillHandler: (item: BrowserCredentialListItem) => Promise<void>
  ): void {
    this.hide();

    if (targetFields.length === 0) {
      return;
    }

    this.credentialItem = credentialItem;
    this.fillHandler = fillHandler;

    for (const targetField of targetFields) {
      const button = document.createElement("button");
      button.type = "button";
      button.textContent = "FB";
      button.title = "Fill with FluentBitwarden";
      button.setAttribute("aria-label", "Fill with FluentBitwarden");
      button.dataset.fluentBitwardenAutofill = "true";
      applyButtonStyles(button);

      button.addEventListener("mousedown", (event) => {
        event.preventDefault();
      });

      button.addEventListener("click", (event) => {
        event.preventDefault();
        event.stopPropagation();
        void this.handleButtonClick(button);
      });

      document.documentElement.append(button);
      this.buttons.push({ button, targetField });
    }

    this.repositionButtons();
  }

  public hide(): void {
    for (const { button } of this.buttons) {
      button.remove();
    }

    this.buttons.length = 0;
    this.fillHandler = null;
    this.credentialItem = null;
  }

  private readonly handleButtonClick = async (button: HTMLButtonElement): Promise<void> => {
    if (!this.fillHandler || !this.credentialItem || button.disabled) {
      return;
    }

    button.disabled = true;

    try {
      await this.fillHandler(this.credentialItem);
    } finally {
      if (button.isConnected) {
        button.disabled = false;
      }
    }
  };

  private readonly repositionButtons = (): void => {
    for (const entry of this.buttons) {
      positionButton(entry.button, entry.targetField);
    }
  };
}

function applyButtonStyles(button: HTMLButtonElement): void {
  button.style.position = "absolute";
  button.style.width = `${ButtonSize}px`;
  button.style.height = `${ButtonSize}px`;
  button.style.minWidth = `${ButtonSize}px`;
  button.style.minHeight = `${ButtonSize}px`;
  button.style.padding = "0";
  button.style.border = "1px solid #0b5cad";
  button.style.borderRadius = "4px";
  button.style.background = "#0f6cbd";
  button.style.color = "#ffffff";
  button.style.font = "600 11px/1 Arial, sans-serif";
  button.style.textAlign = "center";
  button.style.cursor = "pointer";
  button.style.boxShadow = "0 1px 4px rgba(0, 0, 0, 0.24)";
  button.style.zIndex = "2147483647";
}

function positionButton(button: HTMLButtonElement, targetField: HTMLInputElement): void {
  if (!targetField.isConnected) {
    button.style.display = "none";
    return;
  }

  const rect = targetField.getBoundingClientRect();
  if (rect.width < 1 || rect.height < 1) {
    button.style.display = "none";
    return;
  }

  const top = window.scrollY + rect.top + Math.max(0, (rect.height - ButtonSize) / 2);
  const pageRight = window.scrollX + window.innerWidth;
  const outsideLeft = window.scrollX + rect.right + ButtonGap;
  const insideLeft = window.scrollX + rect.right - ButtonSize - ButtonGap;
  const left = outsideLeft + ButtonSize <= pageRight ? outsideLeft : insideLeft;

  button.style.display = "block";
  button.style.top = `${Math.max(0, top)}px`;
  button.style.left = `${Math.max(0, left)}px`;
}

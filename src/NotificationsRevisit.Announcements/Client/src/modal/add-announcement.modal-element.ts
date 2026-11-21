import { css, customElement, html, nothing, query, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import { UMB_NOTIFICATION_CONTEXT } from "@umbraco-cms/backoffice/notification";
import type { UUIInputElement } from "@umbraco-cms/backoffice/external/uui";
import { AnnouncementsService } from "../api/index.js";
import type { AddAnnouncementModalData, AddAnnouncementModalValue } from "./add-announcement.modal-token.js";

type Field = "shortName" | "heading";

/**
 * The add announcement sidebar: short name, heading, main text and a "What is a push notification?"
 * explanation. It sends the request itself, so it can show a busy state and keep the editor's input
 * when something goes wrong. 2018: umb-overlay with umb-toggle switches for three channels.
 */
@customElement("add-announcement-modal")
export class AddAnnouncementModalElement extends UmbModalBaseElement<AddAnnouncementModalData, AddAnnouncementModalValue> {
  @state() private _shortName = "";
  @state() private _heading = "";
  @state() private _mainText = "";
  @state() private _errors: Partial<Record<Field, string>> = {};
  @state() private _serverError?: string;
  @state() private _busy = false;

  @query("#shortName") private _shortNameInput?: UUIInputElement;
  @query("#heading") private _headingInput?: UUIInputElement;

  #notifications?: typeof UMB_NOTIFICATION_CONTEXT.TYPE;

  constructor() {
    super();
    this.consumeContext(UMB_NOTIFICATION_CONTEXT, (context) => {
      this.#notifications = context;
    });
  }

  override firstUpdated(): void {
    // Put keyboard and screen reader users straight into the form when the sidebar opens.
    requestAnimationFrame(() => this._shortNameInput?.focus());
  }

  #validate(): boolean {
    const errors: Partial<Record<Field, string>> = {};
    if (!this._shortName.trim()) errors.shortName = "Enter a short name.";
    if (!this._heading.trim()) errors.heading = "Enter a heading.";
    this._errors = errors;

    // Move focus to the first field that needs attention.
    if (errors.shortName) this._shortNameInput?.focus();
    else if (errors.heading) this._headingInput?.focus();

    return Object.keys(errors).length === 0;
  }

  async #send(event?: Event): Promise<void> {
    event?.preventDefault();

    // Blocks a second submit while the first is running (as well as the button's waiting state).
    if (this._busy) return;

    this._serverError = undefined;
    if (!this.#validate()) return;

    this._busy = true;
    const { data, error } = await AnnouncementsService.createAnnouncement({
      body: { shortName: this._shortName.trim(), heading: this._heading.trim(), mainText: this._mainText.trim() },
    });
    this._busy = false;

    if (error || !data) {
      const detail = (error as { detail?: string } | undefined)?.detail;
      this._serverError = detail ?? "The announcement could not be sent. Please try again.";
      return;
    }

    this.#notifications?.peek("positive", {
      data: {
        headline: "Announcement sent",
        message: `"${this._heading.trim()}" was published and sent to ${data.succeeded} of ${data.attempted} ${data.attempted === 1 ? "subscriber" : "subscribers"}.`,
      },
    });

    this.value = data;
    this._submitModal();
  }

  #input(field: "_shortName" | "_heading" | "_mainText", errorKey?: Field) {
    return (e: Event) => {
      this[field] = (e.target as HTMLInputElement).value;
      if (errorKey && this._errors[errorKey]) {
        this._errors = { ...this._errors, [errorKey]: undefined };
      }
    };
  }

  override render() {
    return html`
      <umb-body-layout headline="Add new announcement">
        <uui-box>
          <form id="announcement-form" novalidate @submit=${(e: Event) => this.#send(e)}>
            ${this._serverError
              ? html`<div class="server-error" role="alert">${this._serverError}</div>`
              : nothing}

            <div class="field">
              <uui-label for="shortName" required>Short name</uui-label>
              <p class="hint" id="shortName-hint">Used in the page name, for example "pool-closed".</p>
              <uui-input
                id="shortName"
                label="Short name"
                required
                maxlength="100"
                .value=${this._shortName}
                ?error=${!!this._errors.shortName}
                aria-invalid=${this._errors.shortName ? "true" : "false"}
                aria-describedby="shortName-hint ${this._errors.shortName ? "shortName-error" : ""}"
                @input=${this.#input("_shortName", "shortName")}
              ></uui-input>
              ${this._errors.shortName
                ? html`<p class="error" id="shortName-error">${this._errors.shortName}</p>`
                : nothing}
            </div>

            <div class="field">
              <uui-label for="heading" required>Heading</uui-label>
              <p class="hint" id="heading-hint">The page heading, and the text of the notification.</p>
              <uui-input
                id="heading"
                label="Heading"
                required
                maxlength="255"
                .value=${this._heading}
                ?error=${!!this._errors.heading}
                aria-invalid=${this._errors.heading ? "true" : "false"}
                aria-describedby="heading-hint ${this._errors.heading ? "heading-error" : ""}"
                @input=${this.#input("_heading", "heading")}
              ></uui-input>
              ${this._errors.heading
                ? html`<p class="error" id="heading-error">${this._errors.heading}</p>`
                : nothing}
            </div>

            <div class="field">
              <uui-label for="mainText">Main text</uui-label>
              <uui-textarea
                id="mainText"
                label="Main text"
                .value=${this._mainText}
                @input=${this.#input("_mainText")}
              ></uui-textarea>
            </div>

            <div class="field">
              <uui-button
                look="secondary"
                compact
                label="What is a push notification?"
                popovertarget="push-explanation"
              >
                <uui-icon name="icon-info"></uui-icon> What is a push notification?
              </uui-button>
              <uui-popover-container id="push-explanation" placement="bottom-start">
                <div class="explanation">
                  A push notification is a short message that appears on the screen of everyone who has
                  turned on notifications on the website, even when the site is not open. They can click
                  it to read the full announcement. It is sent as soon as the page is published.
                </div>
              </uui-popover-container>
            </div>
          </form>
        </uui-box>

        <uui-button slot="actions" label="Cancel" @click=${() => this._rejectModal()}></uui-button>
        <uui-button
          slot="actions"
          look="primary"
          color="positive"
          label="Publish and send"
          type="submit"
          form="announcement-form"
          .state=${this._busy ? "waiting" : undefined}
          ?disabled=${this._busy}
          @click=${(e: Event) => this.#send(e)}
        ></uui-button>
      </umb-body-layout>
    `;
  }

  static override styles = [
    css`
      .field {
        margin-bottom: var(--uui-size-space-5);
      }

      uui-label {
        display: block;
      }

      uui-input,
      uui-textarea {
        width: 100%;
      }

      .hint {
        margin: 0 0 var(--uui-size-space-2);
        color: var(--uui-color-text-alt);
        font-size: var(--uui-type-small-size);
      }

      .error {
        margin: var(--uui-size-space-1) 0 0;
        color: var(--uui-color-danger-standalone);
      }

      .server-error {
        margin-bottom: var(--uui-size-space-5);
        padding: var(--uui-size-space-4);
        border-left: 4px solid var(--uui-color-danger);
        background: var(--uui-color-danger-emphasis, #fbe9e9);
        color: var(--uui-color-danger-contrast, inherit);
      }

      .explanation {
        max-width: 22rem;
        padding: var(--uui-size-space-4);
        background: var(--uui-color-surface);
        border: 1px solid var(--uui-color-border);
        border-radius: var(--uui-border-radius);
        box-shadow: var(--uui-shadow-depth-3);
      }
    `,
  ];
}

export default AddAnnouncementModalElement;

declare global {
  interface HTMLElementTagNameMap {
    "add-announcement-modal": AddAnnouncementModalElement;
  }
}

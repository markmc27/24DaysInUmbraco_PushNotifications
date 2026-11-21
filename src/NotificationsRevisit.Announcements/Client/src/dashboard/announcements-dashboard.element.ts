import { css, customElement, html, nothing, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { umbOpenModal } from "@umbraco-cms/backoffice/modal";
import { AnnouncementsService, type AnnouncementHistoryItem } from "../api/index.js";
import { ADD_ANNOUNCEMENT_MODAL } from "../modal/add-announcement.modal-token.js";

const dateFormat = new Intl.DateTimeFormat("en-GB", { dateStyle: "medium", timeStyle: "short" });

/**
 * The Announcements dashboard in the Content section: subscriber count, history, and a button
 * that opens the add announcement sidebar. 2018: an AngularJS controller and view.
 */
@customElement("announcements-dashboard")
export class AnnouncementsDashboardElement extends UmbLitElement {
  /** Undefined while loading. */
  @state() private _count?: number;

  /** Undefined while loading. */
  @state() private _history?: AnnouncementHistoryItem[];

  @state() private _loadError = false;

  override connectedCallback(): void {
    super.connectedCallback();
    void this.#load();
  }

  async #load(): Promise<void> {
    this._loadError = false;
    const [count, history] = await Promise.all([
      AnnouncementsService.getSubscriberCount(),
      AnnouncementsService.getHistory(),
    ]);

    if (count.error || history.error) {
      this._loadError = true;
      return;
    }

    this._count = count.data?.count ?? 0;
    this._history = history.data ?? [];
  }

  async #openAddAnnouncement(): Promise<void> {
    try {
      await umbOpenModal(this, ADD_ANNOUNCEMENT_MODAL, { data: {} });
    } catch {
      return; // Cancelled: nothing was created or sent.
    }

    // Sent: refresh the count (gone subscriptions may have been removed) and the history.
    this._count = undefined;
    this._history = undefined;
    await this.#load();
  }

  override render() {
    return html`
      <uui-box>
        <div class="header" slot="headline">
          <h2>Announcements</h2>
        </div>
        <div class="summary">
          <p class="count" aria-live="polite" aria-busy=${this._count === undefined ? "true" : "false"}>
            ${this._count === undefined
              ? html`<uui-loader-bar></uui-loader-bar><span class="visually-hidden">Loading subscriber count</span>`
              : html`<strong>${this._count}</strong> ${this._count === 1 ? "subscriber" : "subscribers"} to push notifications`}
          </p>
          <uui-button
            look="primary"
            color="positive"
            label="Add new announcement"
            @click=${() => this.#openAddAnnouncement()}
          ></uui-button>
        </div>
      </uui-box>

      <uui-box headline="Past announcements">${this.#renderHistory()}</uui-box>
    `;
  }

  #renderHistory() {
    if (this._loadError) {
      return html`<p role="alert">The announcements could not be loaded. Refresh the page to try again.</p>`;
    }

    if (this._history === undefined) {
      return html`<uui-loader-bar></uui-loader-bar><span class="visually-hidden">Loading past announcements</span>`;
    }

    if (this._history.length === 0) {
      return html`<p>No announcements have been made yet.</p>`;
    }

    return html`
      <uui-table aria-label="Past announcements, newest first">
        <uui-table-head>
          <uui-table-head-cell>Heading</uui-table-head-cell>
          <uui-table-head-cell>Sent</uui-table-head-cell>
          <uui-table-head-cell>Sent by</uui-table-head-cell>
          <uui-table-head-cell>Delivered</uui-table-head-cell>
          <uui-table-head-cell><span class="visually-hidden">Page</span></uui-table-head-cell>
        </uui-table-head>
        ${this._history.map(
          (item) => html`
            <uui-table-row>
              <uui-table-cell>${item.heading}</uui-table-cell>
              <uui-table-cell>
                <time datetime=${item.sentUtc}>${dateFormat.format(new Date(item.sentUtc))}</time>
              </uui-table-cell>
              <uui-table-cell>${item.userName}</uui-table-cell>
              <uui-table-cell>
                ${item.succeeded} of ${item.attempted}${item.failed > 0 ? html` (${item.failed} failed)` : nothing}
              </uui-table-cell>
              <uui-table-cell>
                <a href="section/content/workspace/document/edit/${item.pageKey}">
                  Edit page<span class="visually-hidden">: ${item.heading}</span>
                </a>
              </uui-table-cell>
            </uui-table-row>
          `,
        )}
      </uui-table>
    `;
  }

  static override styles = [
    css`
      :host {
        display: block;
        padding: var(--uui-size-layout-1);
      }

      uui-box + uui-box {
        margin-top: var(--uui-size-layout-1);
      }

      h2 {
        margin: 0;
        font-size: var(--uui-type-h4-size);
      }

      .summary {
        display: flex;
        flex-wrap: wrap;
        gap: var(--uui-size-space-4);
        align-items: center;
        justify-content: space-between;
      }

      .count {
        margin: 0;
        min-width: 12rem;
      }

      .visually-hidden {
        position: absolute;
        width: 1px;
        height: 1px;
        overflow: hidden;
        clip: rect(0 0 0 0);
        white-space: nowrap;
      }
    `,
  ];
}

export default AnnouncementsDashboardElement;

declare global {
  interface HTMLElementTagNameMap {
    "announcements-dashboard": AnnouncementsDashboardElement;
  }
}

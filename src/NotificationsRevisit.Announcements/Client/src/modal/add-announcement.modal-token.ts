import { UmbModalToken } from "@umbraco-cms/backoffice/modal";
import type { CreateAnnouncementResponse } from "../api/index.js";

export const ADD_ANNOUNCEMENT_MODAL_ALIAS = "NotificationsRevisit.Announcements.Modal.AddAnnouncement";

// eslint-disable-next-line @typescript-eslint/no-empty-object-type
export interface AddAnnouncementModalData {}

/** The modal resolves with the sent announcement; cancelling rejects it. */
export type AddAnnouncementModalValue = CreateAnnouncementResponse;

/** A right-hand sidebar, the modern equivalent of 2018's umb-overlay position="right". */
export const ADD_ANNOUNCEMENT_MODAL = new UmbModalToken<AddAnnouncementModalData, AddAnnouncementModalValue>(
  ADD_ANNOUNCEMENT_MODAL_ALIAS,
  {
    modal: {
      type: "sidebar",
      size: "medium",
    },
  },
);

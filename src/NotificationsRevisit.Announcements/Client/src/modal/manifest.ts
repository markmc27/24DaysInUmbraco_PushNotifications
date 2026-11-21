import { ADD_ANNOUNCEMENT_MODAL_ALIAS } from "./add-announcement.modal-token.js";

export const manifests: Array<UmbExtensionManifest> = [
  {
    type: "modal",
    alias: ADD_ANNOUNCEMENT_MODAL_ALIAS,
    name: "Add Announcement Modal",
    element: () => import("./add-announcement.modal-element.js"),
  },
];

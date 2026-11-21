export const manifests: Array<UmbExtensionManifest> = [
  {
    name: "Announcements Entrypoint",
    alias: "NotificationsRevisit.Announcements.Entrypoint",
    type: "backofficeEntryPoint",
    js: () => import("./entrypoint.js"),
  },
];

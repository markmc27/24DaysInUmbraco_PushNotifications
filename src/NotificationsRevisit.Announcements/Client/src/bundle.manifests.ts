import { manifests as entrypoints } from "./entrypoints/manifest.js";
import { manifests as modals } from "./modal/manifest.js";

// The bundle registers the entry point (API client set-up) and the sidebar modal.
// The dashboard itself is registered in public/umbraco-package.json, so its conditions sit in plain JSON,
// next to where 2018 had Dashboard.config.
export const manifests: Array<UmbExtensionManifest> = [...entrypoints, ...modals];

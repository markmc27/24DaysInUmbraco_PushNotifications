import type { UmbEntryPointOnInit, UmbEntryPointOnUnload } from "@umbraco-cms/backoffice/extension-api";
import { UMB_AUTH_CONTEXT } from "@umbraco-cms/backoffice/auth";
import { client } from "../api/client.gen.js";

// Wire the generated API client into the backoffice auth context (from the umbraco-extension template).
// configureClient() sets the base URL, attaches the bearer token (with refresh) and the default
// response interceptors. The backoffice awaits onInit, so the client is ready before the dashboard loads.
export const onInit: UmbEntryPointOnInit = async (host) => {
  const authContext = await host.getContext(UMB_AUTH_CONTEXT);
  if (!authContext) {
    console.warn("UMB_AUTH_CONTEXT not available: the announcements API client will not be authenticated");
    return;
  }
  authContext.configureClient(client);
};

// Required by the entry point module type; nothing to clean up.
export const onUnload: UmbEntryPointOnUnload = () => {};

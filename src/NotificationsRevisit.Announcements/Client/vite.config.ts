import { defineConfig } from "vite";

export default defineConfig({
  build: {
    lib: {
      // Two entries with stable names:
      // - the bundle (entry point and modal manifests), loaded from umbraco-package.json
      // - the dashboard element, referenced directly by its manifest in umbraco-package.json
      // Shared code (the API client) goes into a hashed chunk both import, so they share one client.
      entry: {
        "notifications-revisit-announcements": "src/bundle.manifests.ts",
        "announcements-dashboard": "src/dashboard/announcements-dashboard.element.ts",
      },
      formats: ["es"],
      fileName: (_format, name) => `${name}.js`,
    },
    outDir: "../wwwroot/App_Plugins/NotificationsRevisitAnnouncements",
    emptyOutDir: true,
    sourcemap: true,
    rollupOptions: {
      external: [/^@umbraco/],
    },
  },
});

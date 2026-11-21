import { defineConfig } from "vite";

// Two entry points with fixed names and no content hashes:
// - wwwroot/sw.js must sit at the site root to control "/", and needs a stable URL,
//   or every build would register as a new service worker.
// - wwwroot/js/push.js is referenced by the site layout.
export default defineConfig({
  publicDir: false,
  build: {
    outDir: "../wwwroot",
    emptyOutDir: false, // wwwroot also holds committed files (favicon, images)
    sourcemap: false,
    rollupOptions: {
      input: {
        push: "src/push.ts",
        sw: "src/sw.ts",
      },
      output: {
        format: "es",
        entryFileNames: (chunk) => (chunk.name === "sw" ? "sw.js" : "js/[name].js"),
      },
    },
  },
});

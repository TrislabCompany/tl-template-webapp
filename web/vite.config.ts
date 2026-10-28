import react from "@vitejs/plugin-react";
import { defineConfig } from "vitest/config";

// The build writes dist/, which the image serves with nginx. The dev server
// sends /api to the local API, as nginx does in the container, so the app
// calls the API at the same relative path everywhere (docs/configuration.md).
// API_UPSTREAM overrides the address of the local API.
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      "/api": process.env.API_UPSTREAM ?? "http://localhost:5080",
    },
  },
  test: {
    environment: "jsdom",
  },
});

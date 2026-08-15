import path from "node:path";
import react from "@vitejs/plugin-react";
import { defineConfig } from "vitest/config";

export default defineConfig({
  plugins: [react()],
  base: "./",
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "src"),
    },
  },
  server: {
    port: 3847,
    strictPort: true,
    proxy: {
      "/voluta": {
        target: "http://localhost:5188",
        changeOrigin: true,
      },
    },
  },
  build: {
    outDir: "../wwwroot/studio",
    emptyOutDir: true,
  },
  test: {
    environment: "jsdom",
    setupFiles: ["./src/test/setup.ts"],
    include: ["src/**/*.{test,spec}.{ts,tsx}"],
    css: false,
    server: {
      deps: {
        // tabster (@fluentui v9 dep) ships broken ESM/CJS named exports;
        // inlining runs it through Vite's interop transform.
        inline: [/tabster/, /@fluentui\//],
      },
    },
  },
});

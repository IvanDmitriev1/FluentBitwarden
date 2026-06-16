import { defineConfig } from "vite";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const rootDirectory = dirname(fileURLToPath(import.meta.url));

export default defineConfig(({ mode }) => ({
  publicDir: "public",
  build: {
    outDir: mode === "firefox" ? "dist/firefox" : "dist/chrome",
    emptyOutDir: true,
    minify: false,
    sourcemap: false,
    target: "chrome120",
    rollupOptions: {
      input: {
        background: resolve(rootDirectory, "src/background/background.ts"),
        content: resolve(rootDirectory, "src/content/content.ts")
      },
      output: {
        entryFileNames: "[name].js",
        chunkFileNames: "chunks/[name]-[hash].js",
        assetFileNames: "assets/[name]-[hash][extname]"
      }
    }
  }
}));

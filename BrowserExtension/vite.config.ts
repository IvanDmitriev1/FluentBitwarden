import { defineConfig, type PluginOption } from "vite";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const rootDirectory = dirname(fileURLToPath(import.meta.url));
const isFirefoxMode = (mode: string) => mode === "firefox";

type BrowserTarget = "chrome" | "firefox";

type ExtensionManifest = {
  manifest_version: 3;
  name: string;
  version: string;
  description: string;
  permissions: string[];
  host_permissions: string[];
  background: { service_worker: string } | { scripts: string[] };
  content_scripts: Array<{
    matches: string[];
    js: string[];
    run_at: "document_idle";
    all_frames: true;
  }>;
  browser_specific_settings?: {
    gecko: {
      id: string;
      data_collection_permissions: {
        required: ["none"];
      };
    };
  };
};

const createManifest = (target: BrowserTarget): ExtensionManifest => {
  const manifest: ExtensionManifest = {
    manifest_version: 3,
    name: "FluentBitwarden",
    version: "0.5.0",
    description: "Browser integration for FluentBitwarden.",
    permissions: ["nativeMessaging"],
    host_permissions: ["http://*/*", "https://*/*"],
    background:
      target === "firefox"
        ? { scripts: ["background.js"] }
        : { service_worker: "background.js" },
    content_scripts: [
      {
        matches: ["http://*/*", "https://*/*"],
        js: ["content.js"],
        run_at: "document_idle",
        all_frames: true
      }
    ]
  };

  if (target === "firefox") {
    manifest.browser_specific_settings = {
      gecko: {
        id: "browser-extension@fluentbitwarden.local",
        data_collection_permissions: {
          required: ["none"]
        }
      }
    };
  }

  return manifest;
};

const manifestPlugin = (target: BrowserTarget): PluginOption => ({
  name: "fluent-bitwarden-manifest",
  generateBundle() {
    this.emitFile({
      type: "asset",
      fileName: "manifest.json",
      source: `${JSON.stringify(createManifest(target), null, 2)}\n`
    });
  }
});

export default defineConfig(({ mode }) => ({
  publicDir: false,
  plugins: [manifestPlugin(isFirefoxMode(mode) ? "firefox" : "chrome")],
  build: {
    outDir: isFirefoxMode(mode) ? "dist/firefox" : "dist/chrome",
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

import { manifests as entrypoints } from "./entrypoints/manifest.js";
import { manifests as marks } from "./marks/manifests.js";
import { manifests as propertyActions } from "./property-actions/manifests.js";
import { manifests as infoApp } from "./info-app/manifests.js";

// umbraco-package.json loads this bundle, which collects the manifests from each part of the extension.
export const manifests: Array<UmbExtensionManifest> = [
  ...entrypoints,
  ...marks,
  ...propertyActions,
  ...infoApp,
  {
    type: "localization",
    alias: "AiProvenance.Localization.En",
    name: "AI Provenance English",
    meta: { culture: "en" },
    js: () => import("./localization/en.js"),
  },
];

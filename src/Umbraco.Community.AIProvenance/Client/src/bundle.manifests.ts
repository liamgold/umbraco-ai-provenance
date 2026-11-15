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
  // Base culture codes, as Umbraco uses for its own translations. A regional backoffice language, such as
  // da-DK, falls back to its base language, and any other language falls back to English.
  {
    type: "localization",
    alias: "AiProvenance.Localization.Da",
    name: "AI Provenance Danish",
    meta: { culture: "da" },
    js: () => import("./localization/da.js"),
  },
  {
    type: "localization",
    alias: "AiProvenance.Localization.De",
    name: "AI Provenance German",
    meta: { culture: "de" },
    js: () => import("./localization/de.js"),
  },
  {
    type: "localization",
    alias: "AiProvenance.Localization.Fr",
    name: "AI Provenance French",
    meta: { culture: "fr" },
    js: () => import("./localization/fr.js"),
  },
  {
    type: "localization",
    alias: "AiProvenance.Localization.Nl",
    name: "AI Provenance Dutch",
    meta: { culture: "nl" },
    js: () => import("./localization/nl.js"),
  },
];

export const manifests: Array<UmbExtensionManifest> = [
  {
    name: "Umbraco Community AIProvenance Entrypoint",
    alias: "Umbraco.Community.AIProvenance.Entrypoint",
    type: "backofficeEntryPoint",
    js: () => import("./entrypoint.js"),
  },
];

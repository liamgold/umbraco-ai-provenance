import { UMB_DOCUMENT_WORKSPACE_ALIAS } from "@umbraco-cms/backoffice/document";
import {
  UMB_WORKSPACE_CONDITION_ALIAS,
  UMB_WORKSPACE_ENTITY_IS_NEW_CONDITION_ALIAS,
} from "@umbraco-cms/backoffice/workspace";

export const manifests: Array<UmbExtensionManifest> = [
  {
    type: "workspaceInfoApp",
    name: "AI Provenance Marks Info App",
    alias: "AiProvenance.WorkspaceInfoApp.Marks",
    element: () => import("./marks-info-app.element.js"),
    weight: 90,
    conditions: [
      { alias: UMB_WORKSPACE_CONDITION_ALIAS, match: UMB_DOCUMENT_WORKSPACE_ALIAS },
      { alias: UMB_WORKSPACE_ENTITY_IS_NEW_CONDITION_ALIAS, match: false },
    ],
  },
];

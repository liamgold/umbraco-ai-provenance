import { UMB_DOCUMENT_WORKSPACE_ALIAS } from "@umbraco-cms/backoffice/document";
import { UMB_WORKSPACE_CONDITION_ALIAS } from "@umbraco-cms/backoffice/workspace";

export const manifests: Array<UmbExtensionManifest> = [
  {
    type: "workspaceContext",
    name: "AI Provenance Document Marks Context",
    alias: "AiProvenance.WorkspaceContext.DocumentMarks",
    api: () => import("./document-marks.context.js"),
    conditions: [{ alias: UMB_WORKSPACE_CONDITION_ALIAS, match: UMB_DOCUMENT_WORKSPACE_ALIAS }],
  },
];

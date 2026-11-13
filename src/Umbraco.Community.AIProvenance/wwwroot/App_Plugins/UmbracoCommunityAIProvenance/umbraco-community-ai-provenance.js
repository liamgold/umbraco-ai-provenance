import { UMB_DOCUMENT_WORKSPACE_ALIAS as n } from "@umbraco-cms/backoffice/document";
import { UMB_WORKSPACE_CONDITION_ALIAS as a, UMB_WORKSPACE_ENTITY_IS_NEW_CONDITION_ALIAS as r } from "@umbraco-cms/backoffice/workspace";
const t = [
  {
    name: "Umbraco Community AIProvenance Entrypoint",
    alias: "Umbraco.Community.AIProvenance.Entrypoint",
    type: "backofficeEntryPoint",
    js: () => import("./entrypoint-BGIdCvDy.js")
  }
], d = [
  {
    type: "workspaceContext",
    name: "AI Provenance Document Marks Context",
    alias: "AiProvenance.WorkspaceContext.DocumentMarks",
    api: () => import("./document-marks.context-qgeU_C77.js").then((e) => e.d),
    conditions: [{ alias: a, match: n }]
  }
], o = "AiProvenance.Condition.MarkState", c = [
  "Umb.PropertyEditorUi.TextBox",
  "Umb.PropertyEditorUi.TextArea",
  "Umb.PropertyEditorUi.Tiptap",
  "Umb.PropertyEditorUi.MarkdownEditor",
  "Umb.PropertyEditorUi.MultipleTextString",
  "Umb.PropertyEditorUi.Tags",
  "Umb.PropertyEditorUi.CodeEditor",
  "Umb.PropertyEditorUi.BlockList",
  "Umb.PropertyEditorUi.BlockGrid",
  "Umb.PropertyEditorUi.BlockSingle",
  "Umb.PropertyEditorUi.MediaPicker",
  "Umb.PropertyEditorUi.ImageCropper",
  "Umb.PropertyEditorUi.UploadField",
  "Umb.PropertyEditorUi.MultiUrlPicker"
], s = [
  { alias: "MarkGenerated", label: "#aiProvenance_markGenerated", icon: "icon-wand", origin: "AiGenerated", showWhen: ["None"] },
  { alias: "MarkModified", label: "#aiProvenance_markModified", icon: "icon-wand", origin: "AiModified", showWhen: ["None"] },
  { alias: "ChangeToModified", label: "#aiProvenance_changeToModified", icon: "icon-wand", origin: "AiModified", showWhen: ["AiGenerated"] },
  { alias: "ChangeToGenerated", label: "#aiProvenance_changeToGenerated", icon: "icon-wand", origin: "AiGenerated", showWhen: ["AiModified"] },
  { alias: "RemoveGenerated", label: "#aiProvenance_removeGenerated", icon: "icon-delete", origin: "None", showWhen: ["AiGenerated"] },
  { alias: "RemoveModified", label: "#aiProvenance_removeModified", icon: "icon-delete", origin: "None", showWhen: ["AiModified"] }
], m = [
  {
    type: "condition",
    name: "AI Provenance Mark State Condition",
    alias: o,
    api: () => import("./mark-state.condition-BVtU_UBr.js")
  },
  ...s.map(
    (e, i) => ({
      type: "propertyAction",
      kind: "default",
      alias: `AiProvenance.PropertyAction.${e.alias}`,
      name: `AI Provenance: ${e.alias}`,
      api: () => import("./set-mark.property-action-DqvEIOo_.js"),
      forPropertyEditorUis: c,
      weight: 50 - i,
      separatorBefore: i === 0 || i === 2,
      meta: {
        icon: e.icon,
        label: e.label,
        origin: e.origin
      },
      conditions: [{ alias: o, oneOf: e.showWhen }]
    })
  )
], p = [
  {
    type: "workspaceInfoApp",
    name: "AI Provenance Marks Info App",
    alias: "AiProvenance.WorkspaceInfoApp.Marks",
    element: () => import("./marks-info-app.element-DacmNcxJ.js"),
    weight: 90,
    conditions: [
      { alias: a, match: n },
      { alias: r, match: !1 }
    ]
  }
], A = [
  ...t,
  ...d,
  ...m,
  ...p,
  {
    type: "localization",
    alias: "AiProvenance.Localization.En",
    name: "AI Provenance English",
    meta: { culture: "en" },
    js: () => import("./en-Ca6wZrxB.js")
  }
];
export {
  A as manifests
};
//# sourceMappingURL=umbraco-community-ai-provenance.js.map

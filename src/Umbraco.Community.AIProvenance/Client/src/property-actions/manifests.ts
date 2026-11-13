import type { AiOrigin } from "../api/index.js";
import { AIP_MARK_STATE_CONDITION_ALIAS } from "./constants.js";

/**
 * The property editors that show the AI mark actions: the ones that hold content an AI could write.
 * The property action manifest has no wildcard, so each editor is listed.
 */
export const AIP_SUPPORTED_PROPERTY_EDITOR_UIS = [
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
  "Umb.PropertyEditorUi.MultiUrlPicker",
];

interface AipActionDefinition {
  alias: string;
  label: string;
  icon: string;
  /** The origin the action sets. */
  origin: AiOrigin;
  /** The current origins for which the action shows. */
  showWhen: Array<AiOrigin>;
}

// The labels say what the property is now, because the property label has no place for a badge.
const actions: Array<AipActionDefinition> = [
  { alias: "MarkGenerated", label: "#aiProvenance_markGenerated", icon: "icon-wand", origin: "AiGenerated", showWhen: ["None"] },
  { alias: "MarkModified", label: "#aiProvenance_markModified", icon: "icon-wand", origin: "AiModified", showWhen: ["None"] },
  { alias: "ChangeToModified", label: "#aiProvenance_changeToModified", icon: "icon-wand", origin: "AiModified", showWhen: ["AiGenerated"] },
  { alias: "ChangeToGenerated", label: "#aiProvenance_changeToGenerated", icon: "icon-wand", origin: "AiGenerated", showWhen: ["AiModified"] },
  { alias: "RemoveGenerated", label: "#aiProvenance_removeGenerated", icon: "icon-delete", origin: "None", showWhen: ["AiGenerated"] },
  { alias: "RemoveModified", label: "#aiProvenance_removeModified", icon: "icon-delete", origin: "None", showWhen: ["AiModified"] },
];

export const manifests: Array<UmbExtensionManifest> = [
  {
    type: "condition",
    name: "AI Provenance Mark State Condition",
    alias: AIP_MARK_STATE_CONDITION_ALIAS,
    api: () => import("./mark-state.condition.js"),
  },
  ...actions.map(
    (action, index): UmbExtensionManifest => ({
      type: "propertyAction",
      kind: "default",
      alias: `AiProvenance.PropertyAction.${action.alias}`,
      name: `AI Provenance: ${action.alias}`,
      api: () => import("./set-mark.property-action.js"),
      forPropertyEditorUis: AIP_SUPPORTED_PROPERTY_EDITOR_UIS,
      weight: 50 - index,
      separatorBefore: index === 0 || index === 2,
      meta: {
        icon: action.icon,
        label: action.label,
        origin: action.origin,
      },
      conditions: [{ alias: AIP_MARK_STATE_CONDITION_ALIAS, oneOf: action.showWhen }],
    }),
  ),
];

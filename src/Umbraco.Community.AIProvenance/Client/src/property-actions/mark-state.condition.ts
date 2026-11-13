import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbConditionBase } from "@umbraco-cms/backoffice/extension-registry";
import type {
  UmbConditionConfigBase,
  UmbConditionControllerArguments,
  UmbExtensionCondition,
} from "@umbraco-cms/backoffice/extension-api";
import { UMB_PROPERTY_CONTEXT, type UmbPropertyContext } from "@umbraco-cms/backoffice/property";
import { UMB_WORKSPACE_CONTEXT } from "@umbraco-cms/backoffice/workspace";
import { observeMultiple } from "@umbraco-cms/backoffice/observable-api";
import type { AiOrigin, MarkResponseModel } from "../api/index.js";
import { AIP_DOCUMENT_MARKS_CONTEXT } from "../marks/document-marks.context.js";
import { referenceFromContexts, sameProperty, type AipWorkspaceInfo } from "../marks/property-reference.js";
import { AIP_MARK_STATE_CONDITION_ALIAS } from "./constants.js";

export interface AipMarkStateConditionConfig extends UmbConditionConfigBase<typeof AIP_MARK_STATE_CONDITION_ALIAS> {
  /** The property's current origin must be one of these. */
  oneOf: Array<AiOrigin>;
}

/**
 * Permits a property action only while the property's current mark is one of the configured origins.
 * Outside a document workspace there are no marks, so the condition never permits.
 */
export class AipMarkStateCondition
  extends UmbConditionBase<AipMarkStateConditionConfig>
  implements UmbExtensionCondition
{
  #property?: UmbPropertyContext;
  #workspace?: AipWorkspaceInfo;
  #marks: Array<MarkResponseModel> = [];

  constructor(host: UmbControllerHost, args: UmbConditionControllerArguments<AipMarkStateConditionConfig>) {
    super(host, args);

    // The nearest workspace is the block workspace for a property inside a block, which gives its element key.
    this.consumeContext(UMB_WORKSPACE_CONTEXT, (workspace) => {
      this.#workspace = workspace as AipWorkspaceInfo | undefined;
      this.#update();
    });

    this.consumeContext(UMB_PROPERTY_CONTEXT, (property) => {
      this.#property = property;
      this.observe(
        property ? observeMultiple([property.alias, property.variantId]) : undefined,
        () => this.#update(),
        "observeProperty",
      );
    });

    this.consumeContext(AIP_DOCUMENT_MARKS_CONTEXT, (marksContext) => {
      this.observe(
        marksContext?.marks,
        (marks) => {
          this.#marks = marks ?? [];
          this.#update();
        },
        "observeMarks",
      );
    });
  }

  #update(): void {
    const property = this.#property ? referenceFromContexts(this.#property, this.#workspace) : undefined;
    if (!property) {
      this.permitted = false;
      return;
    }

    const origin = this.#marks.find((mark) => sameProperty(mark, property))?.origin ?? "None";
    this.permitted = this.config.oneOf.includes(origin);
  }
}

export { AipMarkStateCondition as api };

declare global {
  interface UmbExtensionConditionConfigMap {
    AipMarkStateConditionConfig: AipMarkStateConditionConfig;
  }
}

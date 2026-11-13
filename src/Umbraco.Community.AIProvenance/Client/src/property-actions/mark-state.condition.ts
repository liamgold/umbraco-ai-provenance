import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbConditionBase } from "@umbraco-cms/backoffice/extension-registry";
import type {
  UmbConditionConfigBase,
  UmbConditionControllerArguments,
  UmbExtensionCondition,
} from "@umbraco-cms/backoffice/extension-api";
import { UMB_PROPERTY_CONTEXT } from "@umbraco-cms/backoffice/property";
import { observeMultiple } from "@umbraco-cms/backoffice/observable-api";
import type { AiOrigin } from "../api/index.js";
import { AIP_DOCUMENT_MARKS_CONTEXT } from "../marks/document-marks.context.js";
import { referenceFromPropertyContext, sameProperty } from "../marks/property-reference.js";

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
  constructor(host: UmbControllerHost, args: UmbConditionControllerArguments<AipMarkStateConditionConfig>) {
    super(host, args);

    this.consumeContext(UMB_PROPERTY_CONTEXT, (propertyContext) => {
      this.consumeContext(AIP_DOCUMENT_MARKS_CONTEXT, (marksContext) => {
        if (!propertyContext || !marksContext) {
          this.permitted = false;
          return;
        }

        this.observe(
          observeMultiple([marksContext.marks, propertyContext.alias, propertyContext.variantId, propertyContext.dataPath]),
          ([marks]) => {
            const property = referenceFromPropertyContext(propertyContext);
            if (!property) {
              this.permitted = false;
              return;
            }

            const origin = marks.find((mark) => sameProperty(mark, property))?.origin ?? "None";
            this.permitted = this.config.oneOf.includes(origin);
          },
          "observeMarkState",
        );
      });
    });
  }
}

export { AipMarkStateCondition as api };

declare global {
  interface UmbExtensionConditionConfigMap {
    AipMarkStateConditionConfig: AipMarkStateConditionConfig;
  }
}

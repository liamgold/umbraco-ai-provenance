import { UmbPropertyActionBase } from "@umbraco-cms/backoffice/property-action";
import type { AiOrigin } from "../api/index.js";
import { AIP_DOCUMENT_MARKS_CONTEXT } from "../marks/document-marks.context.js";
import { resolvePropertyReference } from "../marks/property-reference.js";

export interface AipSetMarkPropertyActionMeta {
  /** The origin to set. "None" removes the mark. */
  origin: AiOrigin;
}

/** Sets or removes the AI mark of the property that the action belongs to. */
export class AipSetMarkPropertyAction extends UmbPropertyActionBase<AipSetMarkPropertyActionMeta> {
  override async execute(): Promise<void> {
    const property = await resolvePropertyReference(this);
    const marksContext = await this.getContext(AIP_DOCUMENT_MARKS_CONTEXT);
    if (!property || !marksContext) return;

    await marksContext.setMark(property, this.args.meta.origin);
  }
}

export { AipSetMarkPropertyAction as api };

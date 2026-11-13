import { UmbContextBase } from "@umbraco-cms/backoffice/class-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UMB_DOCUMENT_WORKSPACE_CONTEXT } from "@umbraco-cms/backoffice/document";
import { UmbArrayState } from "@umbraco-cms/backoffice/observable-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import {
  getDocumentByIdMarks,
  putDocumentByIdMarks,
  type AiOrigin,
  type MarkResponseModel,
} from "../api/index.js";
import { sameProperty, type AipPropertyReference } from "./property-reference.js";

/**
 * Holds the draft marks of the open document, for the property actions, the conditions and the Info view.
 * Registered as a workspace context, so blocks opened from the document find it too.
 */
export class AipDocumentMarksContext extends UmbContextBase {
  #documentKey?: string;
  #marks = new UmbArrayState<MarkResponseModel>([], (mark) => markKey(mark));

  /** Every draft mark of the document, in all cultures, segments and blocks. */
  readonly marks = this.#marks.asObservable();

  constructor(host: UmbControllerHost) {
    super(host, AIP_DOCUMENT_MARKS_CONTEXT);

    this.consumeContext(UMB_DOCUMENT_WORKSPACE_CONTEXT, (workspace) => {
      this.observe(
        workspace?.unique,
        (unique) => {
          if (unique && unique !== this.#documentKey) {
            this.#documentKey = unique;
            this.reload();
          }
        },
        "observeDocumentUnique",
      );
    });
  }

  /** Loads the document's marks from the server. */
  async reload(): Promise<void> {
    const documentKey = this.#documentKey;
    if (!documentKey) return;

    const { data } = await tryExecute(this, getDocumentByIdMarks({ path: { id: documentKey } }));
    if (data && documentKey === this.#documentKey) {
      this.#marks.setValue(data);
    }
  }

  /** Returns the origin of one property value. */
  originOf(property: AipPropertyReference): AiOrigin {
    return this.#marks.getValue().find((mark) => sameProperty(mark, property))?.origin ?? "None";
  }

  /** Sets or removes (with "None") the draft mark of one property value. */
  async setMark(property: AipPropertyReference, origin: AiOrigin): Promise<void> {
    const documentKey = this.#documentKey;
    if (!documentKey) return;

    const { error } = await tryExecute(
      this,
      putDocumentByIdMarks({
        path: { id: documentKey },
        body: {
          propertyAlias: property.propertyAlias,
          culture: property.culture,
          segment: property.segment,
          elementKey: property.elementKey,
          origin,
        },
      }),
    );

    if (!error) {
      await this.reload();
    }
  }
}

export const AIP_DOCUMENT_MARKS_CONTEXT = new UmbContextToken<AipDocumentMarksContext>(
  "AiProvenance.DocumentMarksContext",
);

export { AipDocumentMarksContext as api };

function markKey(mark: MarkResponseModel): string {
  return [mark.elementKey ?? "", mark.propertyAlias.toLowerCase(), mark.culture ?? "", mark.segment ?? ""].join("|");
}

import { css, customElement, html, nothing, repeat, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UMB_DOCUMENT_WORKSPACE_CONTEXT } from "@umbraco-cms/backoffice/document";
import type { MarkResponseModel } from "../api/index.js";
import { AIP_DOCUMENT_MARKS_CONTEXT } from "../marks/document-marks.context.js";

/** Lists the document's marked properties in its Info view. */
@customElement("aip-marks-info-app")
export class AipMarksInfoAppElement extends UmbLitElement {
  @state()
  private _marks: Array<MarkResponseModel> = [];

  @state()
  private _propertyNames = new Map<string, string>();

  constructor() {
    super();

    this.consumeContext(AIP_DOCUMENT_MARKS_CONTEXT, (context) => {
      this.observe(context?.marks, (marks) => (this._marks = marks ?? []), "observeMarks");
    });

    this.consumeContext(UMB_DOCUMENT_WORKSPACE_CONTEXT, (workspace) => {
      this.observe(
        workspace?.structure.contentTypeProperties,
        (properties) =>
          (this._propertyNames = new Map(
            (properties ?? []).map((property) => [property.alias.toLowerCase(), property.name ?? property.alias]),
          )),
        "observeProperties",
      );
    });
  }

  #propertyName(mark: MarkResponseModel): string {
    const name = this._propertyNames.get(mark.propertyAlias.toLowerCase()) ?? mark.propertyAlias;
    return mark.elementKey ? this.localize.term("aiProvenance_inBlock", name) : name;
  }

  #details(mark: MarkResponseModel): string {
    return [mark.culture, mark.segment].filter(Boolean).join(" · ");
  }

  override render() {
    return html`
      <uui-box headline=${this.localize.term("aiProvenance_infoHeadline")}>
        ${this._marks.length === 0
          ? html`<p class="empty">${this.localize.term("aiProvenance_noMarks")}</p>`
          : html`
              <ul>
                ${repeat(
                  this._marks,
                  (mark) => `${mark.elementKey}|${mark.propertyAlias}|${mark.culture}|${mark.segment}`,
                  (mark) => html`
                    <li>
                      <span class="name">${this.#propertyName(mark)}</span>
                      ${this.#details(mark) ? html`<span class="details">${this.#details(mark)}</span>` : nothing}
                      <uui-tag look="secondary">
                        ${this.localize.term(
                          mark.origin === "AiGenerated" ? "aiProvenance_aiGenerated" : "aiProvenance_aiModified",
                        )}
                      </uui-tag>
                    </li>
                  `,
                )}
              </ul>
            `}
      </uui-box>
    `;
  }

  static override styles = css`
    ul {
      list-style: none;
      margin: 0;
      padding: 0;
    }

    li {
      display: flex;
      align-items: center;
      gap: var(--uui-size-space-3);
      padding: var(--uui-size-space-2) 0;
      border-bottom: 1px solid var(--uui-color-divider);
    }

    li:last-child {
      border-bottom: none;
    }

    .name {
      font-weight: 600;
    }

    .details {
      color: var(--uui-color-text-alt);
    }

    uui-tag {
      margin-left: auto;
    }

    .empty {
      margin: 0;
      color: var(--uui-color-text-alt);
    }
  `;
}

export default AipMarksInfoAppElement;

declare global {
  interface HTMLElementTagNameMap {
    "aip-marks-info-app": AipMarksInfoAppElement;
  }
}

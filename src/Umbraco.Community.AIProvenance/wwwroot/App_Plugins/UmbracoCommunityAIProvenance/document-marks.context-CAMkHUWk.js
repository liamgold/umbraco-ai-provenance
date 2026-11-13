import { UmbContextBase as c } from "@umbraco-cms/backoffice/class-api";
import { UmbContextToken as l } from "@umbraco-cms/backoffice/context-api";
import { UMB_DOCUMENT_WORKSPACE_CONTEXT as m } from "@umbraco-cms/backoffice/document";
import { UmbArrayState as y } from "@umbraco-cms/backoffice/observable-api";
import { tryExecute as o } from "@umbraco-cms/backoffice/resources";
import { c as a } from "./client.gen-C4MoLlwI.js";
import { UMB_PROPERTY_CONTEXT as d } from "@umbraco-cms/backoffice/property";
const p = (e) => (e.client ?? a).get({
  security: [{ scheme: "bearer", type: "http" }],
  url: "/umbraco/umbracocommunityaiprovenance/api/v1/document/{id}/marks",
  ...e
}), h = (e) => (e.client ?? a).put({
  security: [{ scheme: "bearer", type: "http" }],
  url: "/umbraco/umbracocommunityaiprovenance/api/v1/document/{id}/marks",
  ...e,
  headers: {
    "Content-Type": "application/json",
    ...e.headers
  }
}), f = /@\.key\s*==\s*'([0-9a-fA-F-]{36})'/;
function C(e) {
  const t = e.getAlias();
  if (!t) return;
  const n = e.getVariantId(), r = e.getDataPath()?.match(f)?.[1] ?? null;
  return {
    propertyAlias: t,
    culture: n?.culture ?? null,
    segment: n?.segment ?? null,
    elementKey: r?.toLowerCase() ?? null
  };
}
async function E(e) {
  const t = await e.getContext(d);
  return t ? C(t) : void 0;
}
function g(e, t) {
  return e.propertyAlias.toLowerCase() === t.propertyAlias.toLowerCase() && (e.culture ?? null) === (t.culture?.toLowerCase() ?? null) && (e.segment ?? null) === (t.segment ?? null) && (e.elementKey?.toLowerCase() ?? null) === (t.elementKey ?? null);
}
class s extends c {
  constructor(t) {
    super(t, i), this.#t = new y([], (n) => A(n)), this.marks = this.#t.asObservable(), this.consumeContext(m, (n) => {
      this.observe(
        n?.unique,
        (r) => {
          r && r !== this.#e && (this.#e = r, this.reload());
        },
        "observeDocumentUnique"
      );
    });
  }
  #e;
  #t;
  /** Loads the document's marks from the server. */
  async reload() {
    const t = this.#e;
    if (!t) return;
    const { data: n } = await o(this, p({ path: { id: t } }));
    n && t === this.#e && this.#t.setValue(n);
  }
  /** Returns the origin of one property value. */
  originOf(t) {
    return this.#t.getValue().find((n) => g(n, t))?.origin ?? "None";
  }
  /** Sets or removes (with "None") the draft mark of one property value. */
  async setMark(t, n) {
    const r = this.#e;
    if (!r) return;
    const { error: u } = await o(
      this,
      h({
        path: { id: r },
        body: {
          propertyAlias: t.propertyAlias,
          culture: t.culture,
          segment: t.segment,
          elementKey: t.elementKey,
          origin: n
        }
      })
    );
    u || await this.reload();
  }
}
const i = new l(
  "AiProvenance.DocumentMarksContext"
);
function A(e) {
  return [e.elementKey ?? "", e.propertyAlias.toLowerCase(), e.culture ?? "", e.segment ?? ""].join("|");
}
const O = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
  __proto__: null,
  AIP_DOCUMENT_MARKS_CONTEXT: i,
  AipDocumentMarksContext: s,
  api: s
}, Symbol.toStringTag, { value: "Module" }));
export {
  i as A,
  E as a,
  O as d,
  C as r,
  g as s
};
//# sourceMappingURL=document-marks.context-CAMkHUWk.js.map

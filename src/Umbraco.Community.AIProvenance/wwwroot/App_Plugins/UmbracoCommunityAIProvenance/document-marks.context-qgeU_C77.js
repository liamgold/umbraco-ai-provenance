import { UmbContextBase as u } from "@umbraco-cms/backoffice/class-api";
import { UmbContextToken as l } from "@umbraco-cms/backoffice/context-api";
import { UMB_DOCUMENT_WORKSPACE_CONTEXT as m } from "@umbraco-cms/backoffice/document";
import { UmbArrayState as p } from "@umbraco-cms/backoffice/observable-api";
import { tryExecute as s } from "@umbraco-cms/backoffice/resources";
import { c as i } from "./client.gen-C4MoLlwI.js";
import { UMB_PROPERTY_CONTEXT as y } from "@umbraco-cms/backoffice/property";
import { UMB_WORKSPACE_CONTEXT as d } from "@umbraco-cms/backoffice/workspace";
const C = (e) => (e.client ?? i).get({
  security: [{ scheme: "bearer", type: "http" }],
  url: "/umbraco/umbracocommunityaiprovenance/api/v1/document/{id}/marks",
  ...e
}), f = (e) => (e.client ?? i).put({
  security: [{ scheme: "bearer", type: "http" }],
  url: "/umbraco/umbracocommunityaiprovenance/api/v1/document/{id}/marks",
  ...e,
  headers: {
    "Content-Type": "application/json",
    ...e.headers
  }
}), h = "Umb.Workspace.Block";
function g(e, t) {
  const n = e.getAlias();
  if (!n) return;
  const r = e.getVariantId(), o = t?.workspaceAlias === h ? t.getUnique() ?? null : null;
  return {
    propertyAlias: n,
    culture: r?.culture ?? null,
    segment: r?.segment ?? null,
    elementKey: o?.toLowerCase() ?? null
  };
}
async function E(e) {
  const t = await e.getContext(y), n = await e.getContext(d);
  return t ? g(t, n) : void 0;
}
function A(e, t) {
  return e.propertyAlias.toLowerCase() === t.propertyAlias.toLowerCase() && (e.culture ?? null) === (t.culture?.toLowerCase() ?? null) && (e.segment ?? null) === (t.segment ?? null) && (e.elementKey?.toLowerCase() ?? null) === (t.elementKey ?? null);
}
class a extends u {
  constructor(t) {
    super(t, c), this.#t = new p([], (n) => b(n)), this.marks = this.#t.asObservable(), this.consumeContext(m, (n) => {
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
    const { data: n } = await s(this, C({ path: { id: t } }));
    n && t === this.#e && this.#t.setValue(n);
  }
  /** Returns the origin of one property value. */
  originOf(t) {
    return this.#t.getValue().find((n) => A(n, t))?.origin ?? "None";
  }
  /** Sets or removes (with "None") the draft mark of one property value. */
  async setMark(t, n) {
    const r = this.#e;
    if (!r) return;
    const { error: o } = await s(
      this,
      f({
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
    o || await this.reload();
  }
}
const c = new l(
  "AiProvenance.DocumentMarksContext"
);
function b(e) {
  return [e.elementKey ?? "", e.propertyAlias.toLowerCase(), e.culture ?? "", e.segment ?? ""].join("|");
}
const U = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
  __proto__: null,
  AIP_DOCUMENT_MARKS_CONTEXT: c,
  AipDocumentMarksContext: a,
  api: a
}, Symbol.toStringTag, { value: "Module" }));
export {
  c as A,
  E as a,
  U as d,
  g as r,
  A as s
};
//# sourceMappingURL=document-marks.context-qgeU_C77.js.map

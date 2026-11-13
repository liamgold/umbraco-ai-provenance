import { html as i, repeat as v, nothing as f, css as y, state as _, customElement as g } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement as A } from "@umbraco-cms/backoffice/lit-element";
import { UMB_DOCUMENT_WORKSPACE_CONTEXT as C } from "@umbraco-cms/backoffice/document";
import { A as M } from "./document-marks.context-qgeU_C77.js";
var P = Object.defineProperty, E = Object.getOwnPropertyDescriptor, h = (e) => {
  throw TypeError(e);
}, m = (e, t, a, n) => {
  for (var s = n > 1 ? void 0 : n ? E(t, a) : t, l = e.length - 1, p; l >= 0; l--)
    (p = e[l]) && (s = (n ? p(t, a, s) : p(s)) || s);
  return n && s && P(t, a, s), s;
}, $ = (e, t, a) => t.has(e) || h("Cannot " + a), x = (e, t, a) => t.has(e) ? h("Cannot add the same private member more than once") : t instanceof WeakSet ? t.add(e) : t.set(e, a), c = (e, t, a) => ($(e, t, "access private method"), a), o, d, u;
let r = class extends A {
  constructor() {
    super(), x(this, o), this._marks = [], this._propertyNames = /* @__PURE__ */ new Map(), this.consumeContext(M, (e) => {
      this.observe(e?.marks, (t) => this._marks = t ?? [], "observeMarks");
    }), this.consumeContext(C, (e) => {
      this.observe(
        e?.structure.contentTypeProperties,
        (t) => this._propertyNames = new Map(
          (t ?? []).map((a) => [a.alias.toLowerCase(), a.name ?? a.alias])
        ),
        "observeProperties"
      );
    });
  }
  render() {
    return i`
      <uui-box headline=${this.localize.term("aiProvenance_infoHeadline")}>
        ${this._marks.length === 0 ? i`<p class="empty">${this.localize.term("aiProvenance_noMarks")}</p>` : i`
              <ul>
                ${v(
      this._marks,
      (e) => `${e.elementKey}|${e.propertyAlias}|${e.culture}|${e.segment}`,
      (e) => i`
                    <li>
                      <span class="name">${c(this, o, d).call(this, e)}</span>
                      ${c(this, o, u).call(this, e) ? i`<span class="details">${c(this, o, u).call(this, e)}</span>` : f}
                      <uui-tag look="secondary">
                        ${this.localize.term(
        e.origin === "AiGenerated" ? "aiProvenance_aiGenerated" : "aiProvenance_aiModified"
      )}
                      </uui-tag>
                    </li>
                  `
    )}
              </ul>
            `}
      </uui-box>
    `;
  }
};
o = /* @__PURE__ */ new WeakSet();
d = function(e) {
  const t = this._propertyNames.get(e.propertyAlias.toLowerCase()) ?? e.propertyAlias;
  return e.elementKey ? this.localize.term("aiProvenance_inBlock", t) : t;
};
u = function(e) {
  return [e.culture, e.segment].filter(Boolean).join(" · ");
};
r.styles = y`
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
m([
  _()
], r.prototype, "_marks", 2);
m([
  _()
], r.prototype, "_propertyNames", 2);
r = m([
  g("aip-marks-info-app")
], r);
const T = r;
export {
  r as AipMarksInfoAppElement,
  T as default
};
//# sourceMappingURL=marks-info-app.element-DacmNcxJ.js.map

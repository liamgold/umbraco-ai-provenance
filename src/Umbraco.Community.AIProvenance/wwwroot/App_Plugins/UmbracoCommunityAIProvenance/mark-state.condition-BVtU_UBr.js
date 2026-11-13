import { UmbConditionBase as o } from "@umbraco-cms/backoffice/extension-registry";
import { UMB_PROPERTY_CONTEXT as r } from "@umbraco-cms/backoffice/property";
import { UMB_WORKSPACE_CONTEXT as n } from "@umbraco-cms/backoffice/workspace";
import { observeMultiple as a } from "@umbraco-cms/backoffice/observable-api";
import { A as m, r as h, s as p } from "./document-marks.context-qgeU_C77.js";
class T extends o {
  #t;
  #i;
  #e = [];
  constructor(s, i) {
    super(s, i), this.consumeContext(n, (t) => {
      this.#i = t, this.#s();
    }), this.consumeContext(r, (t) => {
      this.#t = t, this.observe(
        t ? a([t.alias, t.variantId]) : void 0,
        () => this.#s(),
        "observeProperty"
      );
    }), this.consumeContext(m, (t) => {
      this.observe(
        t?.marks,
        (e) => {
          this.#e = e ?? [], this.#s();
        },
        "observeMarks"
      );
    });
  }
  #s() {
    const s = this.#t ? h(this.#t, this.#i) : void 0;
    if (!s) {
      this.permitted = !1;
      return;
    }
    const i = this.#e.find((t) => p(t, s))?.origin ?? "None";
    this.permitted = this.config.oneOf.includes(i);
  }
}
export {
  T as AipMarkStateCondition,
  T as api
};
//# sourceMappingURL=mark-state.condition-BVtU_UBr.js.map

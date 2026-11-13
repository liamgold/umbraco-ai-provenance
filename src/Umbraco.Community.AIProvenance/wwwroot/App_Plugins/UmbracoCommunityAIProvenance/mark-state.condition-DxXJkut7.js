import { UmbConditionBase as m } from "@umbraco-cms/backoffice/extension-registry";
import { UMB_PROPERTY_CONTEXT as f } from "@umbraco-cms/backoffice/property";
import { observeMultiple as c } from "@umbraco-cms/backoffice/observable-api";
import { A as d, r as h, s as p } from "./document-marks.context-CAMkHUWk.js";
class O extends m {
  constructor(t, r) {
    super(t, r), this.consumeContext(f, (i) => {
      this.consumeContext(d, (e) => {
        if (!i || !e) {
          this.permitted = !1;
          return;
        }
        this.observe(
          c([e.marks, i.alias, i.variantId, i.dataPath]),
          ([o]) => {
            const s = h(i);
            if (!s) {
              this.permitted = !1;
              return;
            }
            const a = o.find((n) => p(n, s))?.origin ?? "None";
            this.permitted = this.config.oneOf.includes(a);
          },
          "observeMarkState"
        );
      });
    });
  }
}
export {
  O as AipMarkStateCondition,
  O as api
};
//# sourceMappingURL=mark-state.condition-DxXJkut7.js.map

import { UmbPropertyActionBase as r } from "@umbraco-cms/backoffice/property-action";
import { a, A as o } from "./document-marks.context-CAMkHUWk.js";
class p extends r {
  async execute() {
    const t = await a(this), e = await this.getContext(o);
    !t || !e || await e.setMark(t, this.args.meta.origin);
  }
}
export {
  p as AipSetMarkPropertyAction,
  p as api
};
//# sourceMappingURL=set-mark.property-action-oyvjVvbM.js.map

import { UmbPropertyActionBase as r } from "@umbraco-cms/backoffice/property-action";
import { a, A as o } from "./document-marks.context-qgeU_C77.js";
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
//# sourceMappingURL=set-mark.property-action-DqvEIOo_.js.map

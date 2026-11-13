import { UMB_AUTH_CONTEXT as e } from "@umbraco-cms/backoffice/auth";
import { c as i } from "./client.gen-C4MoLlwI.js";
const r = async (o, t) => {
  const n = await o.getContext(e);
  if (!n) {
    console.warn("UMB_AUTH_CONTEXT not available — extension API client will not be authenticated");
    return;
  }
  n.configureClient(i), console.log("Hello from my extension 🎉");
}, a = (o, t) => {
  console.log("Goodbye from my extension 👋");
};
export {
  r as onInit,
  a as onUnload
};
//# sourceMappingURL=entrypoint-Bm5G_E24.js.map

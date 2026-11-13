const a = [
  {
    name: "Umbraco Community AIProvenance Entrypoint",
    alias: "Umbraco.Community.AIProvenance.Entrypoint",
    type: "backofficeEntryPoint",
    js: () => import("./entrypoint-Bm5G_E24.js")
  }
], n = [
  {
    name: "Umbraco Community AIProvenance Dashboard",
    alias: "Umbraco.Community.AIProvenance.Dashboard",
    type: "dashboard",
    js: () => import("./dashboard.element-DCeFYMWX.js"),
    meta: {
      label: "Example Dashboard",
      pathname: "example-dashboard"
    },
    conditions: [
      {
        alias: "Umb.Condition.SectionAlias",
        match: "Umb.Section.Content"
      }
    ]
  }
], o = [
  ...a,
  ...n
];
export {
  o as manifests
};
//# sourceMappingURL=umbraco-community-ai-provenance.js.map

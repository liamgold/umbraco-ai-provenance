import type {
  UmbEntryPointOnInit,
  UmbEntryPointOnUnload,
} from "@umbraco-cms/backoffice/extension-api";
import { UMB_AUTH_CONTEXT } from "@umbraco-cms/backoffice/auth";
import { client } from "../api/client.gen.js";
import { getConfiguration } from "../api/index.js";
import { AIP_SUPPORTED_PROPERTY_EDITOR_UIS, createMarkActionManifests } from "../property-actions/manifests.js";

export const onInit: UmbEntryPointOnInit = async (host, extensionRegistry) => {
  // Wire the generated API client into the backoffice auth context, so its requests are authenticated.
  // The framework awaits onInit, so the client is configured before any element in this extension uses it.
  const authContext = await host.getContext(UMB_AUTH_CONTEXT);
  if (!authContext) {
    console.warn("UMB_AUTH_CONTEXT not available — extension API client will not be authenticated");
    return;
  }
  authContext.configureClient(client);

  await registerAdditionalPropertyEditors(extensionRegistry);
};

/**
 * Adds the mark actions to the property editors in the AIProvenance:AdditionalPropertyEditorUis setting.
 * The built-in editors keep their actions even when this request fails.
 */
async function registerAdditionalPropertyEditors(
  extensionRegistry: Parameters<UmbEntryPointOnInit>[1],
): Promise<void> {
  try {
    const { data } = await getConfiguration({ throwOnError: false });
    const additional = (data?.additionalPropertyEditorUis ?? []).filter(
      (alias) => !AIP_SUPPORTED_PROPERTY_EDITOR_UIS.includes(alias),
    );

    if (additional.length > 0) {
      extensionRegistry.registerMany(createMarkActionManifests(additional, ".Additional"));
    }
  } catch (error) {
    console.warn("AI Provenance could not load its settings, so only the built-in property editors show the mark actions.", error);
  }
}

export const onUnload: UmbEntryPointOnUnload = () => {};

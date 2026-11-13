import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbClassInterface } from "@umbraco-cms/backoffice/class-api";
import { UMB_PROPERTY_CONTEXT, type UmbPropertyContext } from "@umbraco-cms/backoffice/property";
import { UMB_WORKSPACE_CONTEXT } from "@umbraco-cms/backoffice/workspace";

/** Identifies one property value of the open document. */
export interface AipPropertyReference {
  propertyAlias: string;
  culture: string | null;
  segment: string | null;
  /** The block's element key, for a property inside a block. */
  elementKey: string | null;
}

/** The workspace that edits a block. Its unique is the block's element key. */
const BLOCK_WORKSPACE_ALIAS = "Umb.Workspace.Block";

/** The part of a workspace context that tells a document property from a block property. */
export interface AipWorkspaceInfo {
  workspaceAlias: string;
  getUnique(): string | null | undefined;
}

/**
 * Builds the reference for the property that a property context describes.
 * The workspace is the property's nearest workspace: the block workspace for a property inside a block.
 */
export function referenceFromContexts(
  property: UmbPropertyContext,
  workspace: AipWorkspaceInfo | undefined,
): AipPropertyReference | undefined {
  const propertyAlias = property.getAlias();
  if (!propertyAlias) return undefined;

  const variantId = property.getVariantId();
  const elementKey = workspace?.workspaceAlias === BLOCK_WORKSPACE_ALIAS ? (workspace.getUnique() ?? null) : null;

  return {
    propertyAlias,
    culture: variantId?.culture ?? null,
    segment: variantId?.segment ?? null,
    elementKey: elementKey?.toLowerCase() ?? null,
  };
}

/** Finds the reference for the property that a host (a property action) belongs to. */
export async function resolvePropertyReference(
  host: UmbControllerHost & UmbClassInterface,
): Promise<AipPropertyReference | undefined> {
  const property = await host.getContext(UMB_PROPERTY_CONTEXT);
  const workspace = await host.getContext(UMB_WORKSPACE_CONTEXT);
  return property ? referenceFromContexts(property, workspace as AipWorkspaceInfo | undefined) : undefined;
}

/** Compares a stored mark with a property reference. The server stores cultures in lower case. */
export function sameProperty(
  mark: { propertyAlias: string; culture?: string | null; segment?: string | null; elementKey?: string | null },
  property: AipPropertyReference,
): boolean {
  return (
    mark.propertyAlias.toLowerCase() === property.propertyAlias.toLowerCase() &&
    (mark.culture ?? null) === (property.culture?.toLowerCase() ?? null) &&
    (mark.segment ?? null) === (property.segment ?? null) &&
    (mark.elementKey?.toLowerCase() ?? null) === (property.elementKey ?? null)
  );
}

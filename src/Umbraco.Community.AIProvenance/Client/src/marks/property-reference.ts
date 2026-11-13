import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbClassInterface } from "@umbraco-cms/backoffice/class-api";
import { UMB_PROPERTY_CONTEXT, type UmbPropertyContext } from "@umbraco-cms/backoffice/property";

/** Identifies one property value of the open document. */
export interface AipPropertyReference {
  propertyAlias: string;
  culture: string | null;
  segment: string | null;
  /** The block's element key, for a property inside a block. */
  elementKey: string | null;
}

/** Inside a block, a property's data path contains the block's element key. */
const ELEMENT_KEY_IN_DATA_PATH = /@\.key\s*==\s*'([0-9a-fA-F-]{36})'/;

/** Builds the reference for the property that a property context describes. */
export function referenceFromPropertyContext(context: UmbPropertyContext): AipPropertyReference | undefined {
  const propertyAlias = context.getAlias();
  if (!propertyAlias) return undefined;

  const variantId = context.getVariantId();
  const elementKey = context.getDataPath()?.match(ELEMENT_KEY_IN_DATA_PATH)?.[1] ?? null;

  return {
    propertyAlias,
    culture: variantId?.culture ?? null,
    segment: variantId?.segment ?? null,
    elementKey: elementKey?.toLowerCase() ?? null,
  };
}

/** Finds the reference for the property that a host (a property action or condition) belongs to. */
export async function resolvePropertyReference(
  host: UmbControllerHost & UmbClassInterface,
): Promise<AipPropertyReference | undefined> {
  const context = await host.getContext(UMB_PROPERTY_CONTEXT);
  return context ? referenceFromPropertyContext(context) : undefined;
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

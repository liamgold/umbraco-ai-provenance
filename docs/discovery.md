# AI Provenance: discovery

These notes answer the Phase 0 questions in the [roadmap](roadmap.md). Every finding links to the source it comes from.

- **Umbraco CMS:** tag `release-17.8.1`. Links use `https://github.com/umbraco/Umbraco-CMS/blob/release-17.8.1/`.
- **Umbraco.AI:** branch `v17/main` at commit `bca4f24` (11 Nov 2026). Links use that commit, so they don't move.
- `C/` in a CMS path means `src/Umbraco.Web.UI.Client/src/packages/`.

Checked on 13 Nov 2026.

## Summary

| Question | Answer | Effect on the design |
|---|---|---|
| 1. Versions | Umbraco.AI 17.8.0 needs Umbraco 17.5.0 or later | Code that references Umbraco.AI needs 17.5. The core package can stay at 17.0 |
| 2. Applied suggestions | No event or hook when an editor applies a suggestion | Automatic marking of editor-applied suggestions is not possible in a supported way |
| 3. Agent writes | Server-side, with a detectable AI context that includes the provider and model | Automatic marking of agent writes is workable |
| 4. Backoffice | Property actions work. No supported place for a badge by the label | The indicator needs an alternative |
| 5. Notifications | All present. Publish reports cultures, but not segments | No change |
| 6. Delivery API | Extend the content response builder, inside `properties` | The output sits inside `properties` |
| 7. Management API | Standard package pattern, with the document update permission check | No change |
| 8. Rollback | The rollback notification does not say which version | Rollback needs version snapshots, or stays a known gap |

## 1. Versions

- Umbraco.AI's 17 line on NuGet is **Umbraco.AI 17.8.0**, Prompt 17.4.x and Agent 17.4.x. The 18.x versions are for Umbraco 18.
- Umbraco.AI depends on Umbraco **`[17.5.0, 17.999.999)`** ([`Directory.Packages.props#L17-L21`](https://github.com/umbraco/Umbraco.AI/blob/bca4f24c19ca1d2c4fea82dd955ee0b3f3416b23/Directory.Packages.props#L17-L21)).
- Each product folder splits into `.Core`, `.Web`, `.Web.StaticAssets`, persistence and `.Startup` projects. Package IDs match the project names, for example `Umbraco.AI.Core`.

## 2. Applied suggestions

**Umbraco.AI fires nothing when an editor applies a suggestion.** The whole apply path runs in the browser.

- **Prompt property action.** The action opens the preview modal, waits for the result, and calls the entity adapter's `applyValueChange` ([`prompt-insert.property-action.ts#L187-L197`](https://github.com/umbraco/Umbraco.AI/blob/bca4f24c19ca1d2c4fea82dd955ee0b3f3416b23/Umbraco.AI.Prompt/src/Umbraco.AI.Prompt.Web.StaticAssets/Client/src/prompt/property-actions/prompt-insert.property-action.ts#L187-L197)).
- **Document adapter.** `applyValueChange` reads the existing value, prepares the new one and calls `setPropertyValue` on the document workspace ([`document.adapter.ts#L313-L358`](https://github.com/umbraco/Umbraco.AI/blob/bca4f24c19ca1d2c4fea82dd955ee0b3f3416b23/Umbraco.AI/src/Umbraco.AI.Web.StaticAssets/Client/src/entity-adapter/adapters/document.adapter.ts#L313-L358)). The editor then saves the document as usual.
- **Rich text toolbar.** The TipTap prompt button inserts the text straight into the editor, without the adapter ([`prompts-tiptap-toolbar.element.ts#L336-L351`](https://github.com/umbraco/Umbraco.AI/blob/bca4f24c19ca1d2c4fea82dd955ee0b3f3416b23/Umbraco.AI.Prompt/src/Umbraco.AI.Prompt.Web.StaticAssets/Client/src/prompt/tiptap/prompts-tiptap-toolbar.element.ts#L336-L351)).
- **No event.** A search of the Prompt client, the entity adapters and Copilot found no event, callback, observable or context for an applied value. The only `dispatchEvent` calls are in unrelated tag-input components.
- **Data available at apply time:** document key, property alias, culture, segment, block element key and the previous value. The provider and model are not available in the browser. The prompt execution response has only the content, usage and result options.
- **Server side.** `AIPromptExecutedNotification` carries the document, property alias, culture and segment. It fires when Umbraco.AI generates a suggestion, not when the editor accepts it.

**Options without an upstream change:**

| Option | Problem |
|---|---|
| Replace Umbraco.AI's `document` entity adapter with a wrapper | Depends on Umbraco.AI internals. Misses the rich text toolbar |
| Compare the property value before and after the prompt modal | Misses the rich text toolbar. Fragile |
| Match `AIPromptExecutedNotification` with the next save | Records that AI generated a suggestion, not that the editor used it |

None of these is reliable enough to set a mark the editor did not ask for. The extension point that Umbraco.AI needs is in [Proposed Umbraco.AI extension points](#proposed-umbracoai-extension-points).

## 3. Agent writes

**Agent writes run on the server, and they are detectable.**

- **Write tools** live in `Umbraco.AI.Core/Tools/Umbraco/`: `set_umbraco_content_value`, `update_umbraco_content`, `create_umbraco_content`, publish, unpublish, delete and others. They save through `IContentEditingService` with the current user's key ([`UmbracoWriteAuthorizer.cs`](https://github.com/umbraco/Umbraco.AI/blob/bca4f24c19ca1d2c4fea82dd955ee0b3f3416b23/Umbraco.AI/src/Umbraco.AI.Core/Tools/Umbraco/UmbracoWriteAuthorizer.cs)). Nothing on the save itself marks it as AI.
- **Tool notifications.** Every server-side tool call publishes `AIToolExecutingNotification` and `AIToolExecutedNotification`. They carry the tool, its arguments (document, alias, culture, segment) and the runtime context ([`AIToolNotificationInvoker.cs#L44-L114`](https://github.com/umbraco/Umbraco.AI/blob/bca4f24c19ca1d2c4fea82dd955ee0b3f3416b23/Umbraco.AI/src/Umbraco.AI.Core/Tools/AIToolNotificationInvoker.cs#L44-L114)).
- **Runtime context.** `IAIRuntimeContextAccessor.Context` is public, and it is not null while a tool runs ([`IAIRuntimeContextAccessor.cs`](https://github.com/umbraco/Umbraco.AI/blob/bca4f24c19ca1d2c4fea82dd955ee0b3f3416b23/Umbraco.AI/src/Umbraco.AI.Core/RuntimeContext/IAIRuntimeContextAccessor.cs)). It flows through the async call, so a `ContentSavingNotification` handler in the same flow sees it. **Confirmed on 13 Nov 2026**: an agent on Azure `gpt-5-nano` changed two properties, and the add-on marked both, with the provider and model.
- **Provider and model.** `ScopedProfileChatClient` stores the profile, provider and model in the runtime context ([`ScopedProfileChatClient.cs#L124`](https://github.com/umbraco/Umbraco.AI/blob/bca4f24c19ca1d2c4fea82dd955ee0b3f3416b23/Umbraco.AI/src/Umbraco.AI.Core/Chat/ScopedProfileChatClient.cs#L124)), under the keys `Umbraco.AI.ProviderId` and `Umbraco.AI.ModelId` ([`Constants.cs#L105-L120`](https://github.com/umbraco/Umbraco.AI/blob/bca4f24c19ca1d2c4fea82dd955ee0b3f3416b23/Umbraco.AI/src/Umbraco.AI.Core/Constants.cs#L105-L120)).
- **Automate** runs agents on the server with the same tools, under a service account. The same detection applies.
- **Copilot browser tools** apply values in the browser through the entity adapter, like prompts. They have the same gap as section 2.

**Approach:** handle `ContentSavingNotification`. When `IAIRuntimeContextAccessor.Context` is not null, compare the old and new property values, and mark each changed property with the rules in [`design.md`](design.md#automatic-marking-covers-agent-writes). This needs a reference to `Umbraco.AI.Core`.

The AI disclosure notice from Umbraco.AI PRs #440 and #441 is a global UI setting. It stores nothing per content item.

## 4. Backoffice extension points

### Property actions

- **Manifest:** `type: 'propertyAction'`, with a required `forPropertyEditorUis` list and optional conditions ([`property-action.extension.ts`](https://github.com/umbraco/Umbraco-CMS/blob/release-17.8.1/src/Umbraco.Web.UI.Client/src/packages/core/property-action/property-action.extension.ts)).
- **API:** extend `UmbPropertyActionBase` and implement `execute()`.
- **Property data:** consume `UMB_PROPERTY_CONTEXT` for the alias, value and variant (`getAlias()`, `getValue()`, `getVariantId()`). The built-in clear action does the same ([`property-action-clear.controller.ts`](https://github.com/umbraco/Umbraco-CMS/blob/release-17.8.1/src/Umbraco.Web.UI.Client/src/packages/core/property-action/kinds/clear/property-action-clear.controller.ts)). For the document key, consume `UMB_DOCUMENT_WORKSPACE_CONTEXT` and call `getUnique()`.
- **No wildcard.** The menu matches `forPropertyEditorUis.includes(alias)` ([`property-action-menu.element.ts#L34`](https://github.com/umbraco/Umbraco-CMS/blob/release-17.8.1/src/Umbraco.Web.UI.Client/src/packages/core/property-action/components/property-action-menu/property-action-menu.element.ts#L34)). The package has to list every property editor UI alias it supports. A third-party editor needs adding to the list.
- **Show and hide.** Conditions re-evaluate, as `UmbPropertyHasValueCondition` shows ([`has-value.condition.ts`](https://github.com/umbraco/Umbraco-CMS/blob/release-17.8.1/src/Umbraco.Web.UI.Client/src/packages/core/property/conditions/has-value/has-value.condition.ts)). A custom condition can show only the actions that apply to the current mark.

### A badge by the label

**There is no supported extension point.** `umb-property-layout` has three fixed slots (`action-menu`, `description`, `editor`), and the label itself is not pluggable ([`property-layout.element.ts#L71-L91`](https://github.com/umbraco/Umbraco-CMS/blob/release-17.8.1/src/Umbraco.Web.UI.Client/src/packages/core/property/components/property-layout/property-layout.element.ts#L71-L91)). `umb-property` fills those slots itself ([`property.element.ts#L470-L489`](https://github.com/umbraco/Umbraco-CMS/blob/release-17.8.1/src/Umbraco.Web.UI.Client/src/packages/core/property/components/property/property.element.ts#L470-L489)).

- The `propertyContext` extension type runs code for each property, but it has no rendering hook. Adding an element to the property's description slot from there would rely on Umbraco's internal markup.
- **Supported alternatives:**
  - the property action menu, which shows the current mark
  - a `workspaceInfoApp` that lists the marked properties in the document's Info view (example: [`audit-log/info-app/manifests.ts`](https://github.com/umbraco/Umbraco-CMS/blob/release-17.8.1/src/Umbraco.Web.UI.Client/src/packages/documents/documents/audit-log/info-app/manifests.ts))
  - a workspace view or a workspace footer app

### Blocks

- Properties inside Block List and Block Grid items render through the same `umb-property`, so the property actions appear there too.
- Inside a block, the inner alias comes from `UMB_PROPERTY_CONTEXT`, the block's element key from `UMB_BLOCK_WORKSPACE_CONTEXT`, and the outer property alias from `UMB_BLOCK_MANAGER_CONTEXT`.
- `UMB_DOCUMENT_WORKSPACE_CONTEXT` still resolves inside a block modal. The generic `UMB_WORKSPACE_CONTEXT` returns the block workspace, so don't use it.
- The `blockAction` extension type exists in 17.8.1 ([`block-action.extension.ts`](https://github.com/umbraco/Umbraco-CMS/blob/release-17.8.1/src/Umbraco.Web.UI.Client/src/packages/block/block/action/block-action.extension.ts)). Property actions already cover block properties, so the package does not need it.
- **Effect on the data model:** a block property needs the block's element key as well as the property alias. See [Design changes](#design-changes-to-agree).

### Culture and segment

- `UMB_PROPERTY_DATASET_CONTEXT` gives the culture and segment of the pane that a property is in.
- `UMB_VARIANT_CONTEXT` gives the workspace's active variant.

## 5. Notifications

All in `src/Umbraco.Core/Notifications/`.

| Event | Notification | Notes |
|---|---|---|
| Save | `ContentSavingNotification`, `ContentSavedNotification` | |
| Publish | `ContentPublishingNotification`, `ContentPublishedNotification` | `PublishedCultures` lists the published cultures per document, with `*` for invariant ([`ContentPublishedNotification.cs#L98-L112`](https://github.com/umbraco/Umbraco-CMS/blob/release-17.8.1/src/Umbraco.Core/Notifications/ContentPublishedNotification.cs#L98-L112)). It can be null for descendants. Unpublishing one culture also arrives here, in `UnpublishedCultures` |
| Unpublish | `ContentUnpublishedNotification` | Whole document |
| Copy | `ContentCopiedNotification` | `Original` and `Copy`, so the new key is `Copy.Key` |
| Recycle bin | `ContentMovedToRecycleBinNotification` | Marks stay, in case the document is restored |
| Delete | `ContentDeletedNotification` | Raised for each item, including descendants and an emptied recycle bin |
| Rollback | `ContentRolledBackNotification` | Carries the content only. See section 8 |

No notification carries segments. On publish, the package copies all segment marks for each published culture.

## 6. Content Delivery API

- `IApiContentResponseBuilder` is a singleton. Its default implementation, `ApiContentResponseBuilder`, has a virtual `Create` method ([`ApiContentResponseBuilder.cs#L29`](https://github.com/umbraco/Umbraco-CMS/blob/release-17.8.1/src/Umbraco.Core/DeliveryApi/ApiContentResponseBuilder.cs#L29)).
- A new response type would fail serialization. The JSON type resolver only accepts `ApiContentResponse` ([`ContentJsonTypeResolverBase.cs#L49-L77`](https://github.com/umbraco/Umbraco-CMS/blob/release-17.8.1/src/Umbraco.Infrastructure/Serialization/ContentJsonTypeResolverBase.cs#L49-L77)).
- **Approach:** subclass `ApiContentResponseBuilder`, override `Create`, and add an `aiProvenance` entry to `properties`. Do the same for `IApiContentBuilder` (linked items) and `IApiElementBuilder` (blocks).
- In preview, the Delivery API serves draft content, so the package reads draft marks there.

## 7. Management API

- Controllers extend `ManagementApiControllerBase` ([`ManagementApiControllerBase.cs`](https://github.com/umbraco/Umbraco-CMS/blob/release-17.8.1/src/Umbraco.Cms.Api.Management/Controllers/ManagementApiControllerBase.cs)) and use `[VersionedApiBackOfficeRoute]`.
- The package registers its own Swagger document and uses `[MapToApi]` with its name. The template's example controller and composer already do this.
- **Permission:** the document update endpoint checks `ContentPermissionResource.WithKeys(ActionUpdate.ActionLetter, id)` against `AuthorizationPolicies.ContentPermissionByResource` ([`UpdateDocumentControllerBase.cs#L30-L37`](https://github.com/umbraco/Umbraco-CMS/blob/release-17.8.1/src/Umbraco.Cms.Api.Management/Controllers/Document/UpdateDocumentControllerBase.cs#L30-L37)). A culture-aware overload exists. The package's endpoints use the same check.

## 8. Migrations and rollback

### Migrations

- Use `PackageMigrationPlan` with `To<TMigration>(...)`.
- **`MigrationBase` is obsolete** and goes in Umbraco 18 ([`MigrationBase.cs#L4`](https://github.com/umbraco/Umbraco-CMS/blob/release-17.8.1/src/Umbraco.Infrastructure/Migrations/MigrationBase.cs#L4)). Use `AsyncMigrationBase`, which also eases the move to 18.
- Core example: [`AddRepositoryCacheVersionTable.cs`](https://github.com/umbraco/Umbraco-CMS/blob/release-17.8.1/src/Umbraco.Infrastructure/Migrations/Upgrade/V_17_0_0/AddRepositoryCacheVersionTable.cs), with the DTO attributes in [`RepositoryCacheVersionDto.cs`](https://github.com/umbraco/Umbraco-CMS/blob/release-17.8.1/src/Umbraco.Infrastructure/Persistence/Dtos/RepositoryCacheVersionDto.cs).

### Rollback

- `ContentService.Rollback` copies the old version's values, saves, and publishes `ContentRolledBackNotification` with the content only ([`ContentService.cs#L253-L297`](https://github.com/umbraco/Umbraco-CMS/blob/release-17.8.1/src/Umbraco.Core/Services/ContentService.cs#L253-L297)). Nothing says which version the editor rolled back to.
- `IContentBase.VersionId` exists, so the package could store a snapshot of the marks for each version.
- Restoring a snapshot needs the target version. The package would have to wrap `IContentVersionService.RollBackAsync` or filter the rollback endpoint to capture it.

## Proposed Umbraco.AI extension points

These would make automatic marking reliable. They are the basis for an issue on the Umbraco.AI repo.

1. **A browser hook when an AI value is applied.** One event or extension type, used by the prompt property action, the rich text toolbar and Copilot's browser tools. It carries the entity type and key, property alias, culture, segment, block element key, previous value, and the prompt, profile, provider and model.
2. **Provider and model in the prompt execution response,** so the browser knows which model produced a suggestion.
3. **A server notification for AI content writes,** such as `AIContentWrittenNotification`, with the document key, changed property aliases, culture, segment, tool, profile, provider and model. Section 3's approach works without it, but relies on the runtime context flowing into the save.

## Umbraco.AI behaviour found in testing

- **Approval policy.** Write tools such as `set_umbraco_content_value` do not need approval (`RequiresApproval = false`). The interactive AG-UI stream, which the Copilot chat uses, runs them straight away. The non-interactive `/run` endpoint and Automate use `AIApprovalPolicy.DenyAll` by default, so they skip every write tool unless an agent or automation allows it ([`AIApprovalPolicy.cs`](https://github.com/umbraco/Umbraco.AI/blob/bca4f24c19ca1d2c4fea82dd955ee0b3f3416b23/Umbraco.AI.Agent/src/Umbraco.AI.Agent.Core/Agents/AIApprovalPolicy.cs)).
- **Umbraco.AI.Core on its own.** A site with only `Umbraco.AI.Core` installed fails to start. Umbraco finds Core's property value converters, but their services, such as `IAIContextService`, are only registered by the full `Umbraco.AI` package. The add-on therefore depends on `Umbraco.AI`.
- **Section access.** The Umbraco.AI management API requires the AI section in the access token's allowed-applications claim. Umbraco.AI adds the section to the Admin group on install, but users signed in before that get 403 until they sign out and in again.

## Community feedback

- **Umbraco builder day call, 13 Nov 2026.** The idea was discussed on the call, and the feedback was positive.

## Design changes

These findings changed [`design.md`](design.md) on 13 Nov 2026:

| Finding | Decision |
|---|---|
| No supported badge by the label (section 4) | Show the mark in the property action menu, and list marked properties in the Info view |
| Editor-applied suggestions cannot be detected. Agent writes can (sections 2 and 3) | Automatic marking covers agent writes only. Editors mark applied suggestions themselves |
| Marking agent writes needs `Umbraco.AI.Core` and Umbraco 17.5 (sections 1 and 3) | Put the Umbraco.AI integration in a separate add-on package. The main package stays on 17.0 |
| Rollback does not say which version (section 8) | Rollback does not restore marks. The README lists it as a limitation |
| A block property needs its element key (section 4) | Add `elementKey` to the data model |
| The Delivery API rejects extra fields beside `properties` (section 6) | Put `aiProvenance` inside `properties` |

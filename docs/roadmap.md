# AI Provenance: roadmap

This roadmap lists the work for the first release, in order. Each phase ends with acceptance criteria. A phase is complete when it meets them.

[`design.md`](design.md) describes what the package does and why. [`discovery.md`](discovery.md) records how Umbraco and Umbraco.AI work, with links to their source. This file describes how and in what order the package gets built.

## Before the first release

- Share the design with the Umbraco.AI team. Ask whether they plan AI provenance themselves, and how they recommend a package supports Umbraco 17 and 18+ after the OpenAPI change.
- Open an issue on the Umbraco.AI repo for the [extension points](discovery.md#proposed-umbracoai-extension-points) that would let the package mark editor-applied suggestions.
- Record the answers in [`discovery.md`](discovery.md).

## Phase 0: discovery (done)

[`discovery.md`](discovery.md) answers the eight questions. Its findings changed four decisions in `design.md`:

- Editors see marks in the property action menu and the Info view, not in a badge by the label.
- Automatic marking covers agent writes only.
- The Umbraco.AI integration is a separate package.
- Rollback does not restore marks.

## Phase 1: storage and core service

- Remove the template's example dashboard, API controller and view model.
- Add a package migration plan, with an `AsyncMigrationBase` migration that creates the `aiProvenance` table (see [`design.md`](design.md#data-model)). `MigrationBase` is obsolete in Umbraco 17.
- Add `IAiProvenanceService`, with methods to:
  - get a document's marks (draft or published, for a culture and segment)
  - set or remove one property's mark, including a property inside a block
  - copy draft marks to published
- Add notification handlers that:
  - copy draft marks to published on publish, for each culture in `PublishedCultures`, with all segments
  - treat a missing `PublishedCultures` entry (a descendant published with its parent) as all published cultures
  - remove a culture's published marks when `UnpublishedCultures` lists it, and all published marks when the whole document is unpublished
  - delete all marks when a document is deleted permanently
  - copy marks when a document is copied, to `Copy.Key`
- Add a composer that registers everything.
- Add a test project, `tests/Umbraco.Community.AIProvenance.Tests`, that uses `Umbraco.Cms.Tests.Integration` 17.x.

**Acceptance:** integration tests cover set, get, remove, publish, unpublish, delete and copy, for invariant and variant content and for a property inside a block.

## Phase 2: backoffice marking

- Replace the template's API controller with endpoints to get a document's draft marks, and to set or remove one property's mark. They require the same permission as updating the document (`ActionUpdate` through `ContentPermissionByResource`).
- Add property actions: **Mark as AI generated**, **Mark as AI modified** and **Remove AI mark**.
  - List every supported property editor UI alias in `forPropertyEditorUis`, because the manifest has no wildcard.
  - Add a custom condition, so each property shows only the actions that apply to its current mark.
  - Show the current mark in the menu.
  - Read the alias and variant from `UMB_PROPERTY_CONTEXT`, and the document key from `UMB_DOCUMENT_WORKSPACE_CONTEXT`. Inside a block, read the element key from `UMB_BLOCK_WORKSPACE_CONTEXT`.
- Add a `workspaceInfoApp` that lists the document's marked properties in its Info view.
- Refresh the marks when the editor switches culture or segment, or when a mark changes.

**Acceptance:** an editor can mark, change and remove a mark on a text property, a rich text property and a property inside a block. This works in the default culture and in a second culture. The menu and the Info view update straight away. After publishing, the website sees the mark.

## Phase 3: reading the marks

- Add the Razor extension `GetAiOrigin("propertyAlias")` for the current culture, plus an overload that takes a culture.
- Add the `<ai-label for="..." />` tag helper, with an optional `position` and child content:
  - It renders nothing when the property has no mark.
  - It renders "AI generated" or "AI modified", with a fuller explanation for screen readers and on hover or tap: "AI generated this content." or "Our editors wrote this content and edited it with AI."
  - It uses the European Commission's icons for AI-generated and AI-modified content, after a check of their licence and usage guidance.
  - Sites can translate the text, and override the markup with a partial view such as `Views/Partials/AIProvenance/Label.cshtml`.
- Add an `aiProvenance` entry to `properties` in the Content Delivery API output:
  - Subclass `ApiContentResponseBuilder`, and do the same for `IApiContentBuilder` and `IApiElementBuilder`.
  - List only the marked properties.
  - Read draft marks in preview.
  - A setting turns the output off.

**Acceptance:** a published, marked property shows its label through the tag helper, and appears in the Delivery API response. An unmarked property shows neither. Unit tests cover the extension method and the tag helper output.

## Phase 4: automatic marking with Umbraco.AI

- Add the project `src/Umbraco.Community.AIProvenance.UmbracoAI`, packed as its own NuGet package. It references `Umbraco.AI.Core` 17.x and needs Umbraco 17.5 or later. Nothing else in the solution references Umbraco.AI.
- Add a `ContentSavingNotification` handler. When `IAIRuntimeContextAccessor.Context` is not null:
  - compare the old and new value of each property
  - mark each changed property with the rules in [`design.md`](design.md#automatic-marking-covers-agent-writes), with source `UmbracoAI`
  - store the provider and model from the runtime context
- First, test on the test site that the runtime context reaches the save handler. If it does not, use `AIToolExecutedNotification` and the tool's arguments instead.
- Add a setting that turns automatic marking on or off. It is on by default.

**Acceptance:** an Umbraco.AI agent that writes to an empty property marks it AI generated. An agent that changes a property with content marks it AI modified. The mark stores the provider and model. With only the main package installed, the site starts and manual marking works.

## Phase 5: release

- Fill in the template's placeholders: the `.csproj` description and tags, `.github/README.md`, `docs/README_nuget.md`, and the category in `umbraco-marketplace.json`.
- Add the `umbraco-marketplace` NuGet tag when the packages are ready to list. The template leaves it out on purpose.
- Write the README. It covers:
  - what the packages do and do not do, including "not AI detection" and "does not make a site compliant"
  - which package to install, with and without Umbraco.AI
  - configuration, and the Razor, tag helper and Delivery API usage
  - screenshots of the property actions, the Info view and a front-end label
  - the known limitations: rollback, editor-applied suggestions, and third-party property editors
  - a link to [`design.md`](design.md)
- Add a pull request workflow that builds, tests and packs. Extend the template's `release.yml` to publish both packages.
- Pack both packages to a local NuGet folder. Install them into a clean Umbraco 17 site, and test each package alone and together.

**Acceptance:** `dotnet pack` produces both packages. Each installs and works on a clean Umbraco 17 site. The README covers everything above.

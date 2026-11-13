# AI Provenance: roadmap

This roadmap lists the work for the first release, in order. Each phase ends with acceptance criteria. A phase is complete when it meets them.

[`design.md`](design.md) describes what the package does and why. This file describes how and in what order it gets built.

## Before the first release

- Share the design with the Umbraco.AI team. Ask whether they plan AI provenance themselves, and how they recommend a package supports Umbraco 17 and 18+ after the OpenAPI change.
- Record the answers in [`discovery.md`](discovery.md).

## Phase 0: discovery

Install Umbraco.AI in the test site and configure an AI provider. Then answer these questions in [`discovery.md`](discovery.md), with links to the source or docs:

1. **Versions.** Which Umbraco.AI version supports Umbraco 17, and what is its minimum CMS version?
2. **Applied suggestions.** Where does Umbraco.AI apply a suggestion to a property? Look for a server-side notification, a backoffice event or context, or something else. If the hook is in the backoffice, the adapter can be TypeScript with no NuGet reference to Umbraco.AI.
3. **Agent writes.** Umbraco.AI agents can also write content directly through tools (see [Umbraco.AI issue #666](https://github.com/umbraco/Umbraco.AI/issues/666)). Do agent writes go through the same path as applied suggestions? If not, which hook covers them?
4. **Backoffice extension points:**
   - Can a property action offer "Mark as AI generated", "Mark as AI modified" and "Remove AI mark"?
   - Is there a supported way to show a badge next to a marked property's label? If not, what is the cleanest alternative?
   - Does the `blockAction` extension type ([Umbraco-CMS PR #22459](https://github.com/umbraco/Umbraco-CMS/pull/22459)) help to mark properties inside blocks?
5. **Notifications** for save, publish, unpublish, copy, delete and rollback.
6. **Content Delivery API.** What is the supported way to add data to a content item's output?
7. **Management API.** How does a package add authorised backoffice endpoints on Umbraco 17?
8. **Rollback.** How could marks follow Umbraco's content versions?

**Acceptance:** `discovery.md` answers all eight questions and lists anything that changes the design.

## Phase 1: storage and core service

- Remove the template's example dashboard, API controller and view model.
- Add a migration plan that creates the `aiProvenance` table (see [`design.md`](design.md#data-model)).
- Add `IAiProvenanceService`, with methods to:
  - get a document's marks (draft or published, for a culture and segment)
  - set or remove one property's mark
  - copy draft marks to published
- Add notification handlers that:
  - copy draft marks to published on publish
  - remove published marks on unpublish
  - delete all marks when a document is deleted permanently
  - copy marks when a document is copied
- Add a composer that registers everything.
- Add a test project, `tests/Umbraco.Community.AIProvenance.Tests`, that uses `Umbraco.Cms.Tests.Integration` 17.x.

**Acceptance:** integration tests cover set, get, remove, publish, unpublish, delete and copy, for invariant and variant content.

## Phase 2: backoffice marking

- Add Management API endpoints to get a document's draft marks, and to set or remove one property's mark. They require the same permission as updating the document.
- Add the backoffice extension:
  - property actions: **Mark as AI generated**, **Mark as AI modified** and **Remove AI mark**, showing only the actions that apply
  - an indicator on marked properties: a badge by the label, or the best alternative from Phase 0
- Refresh the marks when the editor switches culture or segment.

**Acceptance:** an editor can mark, change and remove a mark on a text property, a rich text property and a property in a block. This works in the default culture and in a second culture. The badge appears and disappears straight away. After publishing, the website sees the mark.

If editors cannot mark properties inside Block List and Block Grid items one by one, the first release marks the whole block property. The README then lists this as a limitation.

## Phase 3: reading the marks

- Add the Razor extension `GetAiOrigin("propertyAlias")` for the current culture, plus an overload that takes a culture.
- Add the `<ai-label for="..." />` tag helper, with an optional `position` and child content:
  - It renders nothing when the property has no mark.
  - It renders "AI generated" or "AI modified", with a fuller explanation for screen readers and on hover or tap: "AI generated this content." or "Our editors wrote this content and edited it with AI."
  - It uses the European Commission's icons for AI-generated and AI-modified content, after a check of their licence and usage guidance.
  - Sites can translate the text, and override the markup with a partial view such as `Views/Partials/AIProvenance/Label.cshtml`.
- Add an `aiProvenance` object to each item in the Content Delivery API output, with only the marked properties. A setting turns it off.

**Acceptance:** a published, marked property shows its label through the tag helper, and appears in the Delivery API response. An unmarked property shows neither. Unit tests cover the extension method and the tag helper output.

## Phase 4: automatic marking with Umbraco.AI

This phase uses the answers to Phase 0 questions 2 and 3.

- Keep all Umbraco.AI code in one folder and namespace, with its own composer. Nothing outside that folder references Umbraco.AI types. The adapter calls only `IAiProvenanceService`.
- If the hook is in the backoffice, apply the same rule in TypeScript. The adapter gets its own folder and manifest entry, and checks whether Umbraco.AI is installed when the backoffice loads.
- When Umbraco.AI applies a suggestion, set the mark with the rules in [`design.md`](design.md#automatic-marking-follows-two-rules), with source `UmbracoAI`. Store the provider and model.
- Add a setting that turns automatic marking on or off. It is on by default.
- Keep the package working without Umbraco.AI, with manual marking only.
- If the package needs a server-side reference to Umbraco.AI, use a wide version range.

If Umbraco.AI has no usable hook, the package does not patch Umbraco.AI's internals. Instead, `umbraco-ai-extension-point.md` describes the extension point the package needs, as the basis for an issue or pull request on the Umbraco.AI repo. The first release then ships with manual marking only.

**Acceptance:** applying an AI suggestion to an empty property marks it AI generated. Applying one to a property with content marks it AI modified. The editor does nothing else. With Umbraco.AI uninstalled, the site starts and manual marking works.

## Phase 5: release

- Fill in the template's placeholders: the `.csproj` description and tags, `.github/README.md`, `docs/README_nuget.md`, and the category in `umbraco-marketplace.json`.
- Add the `umbraco-marketplace` NuGet tag when the package is ready to list. The template leaves it out on purpose.
- Write the README. It covers:
  - what the package does and does not do, including "not AI detection" and "does not make a site compliant"
  - install, configuration, and the Razor, tag helper and Delivery API usage
  - screenshots of the property actions, the badge and a front-end label
  - the known limitations from the phases above
  - a link to [`design.md`](design.md)
- Add a pull request workflow that builds, tests and packs. The template's `release.yml` already publishes to NuGet when a version tag is pushed.
- Pack the package to a local NuGet folder and install it into a clean Umbraco 17 site.

**Acceptance:** `dotnet pack` produces the package. The package installs and works on a clean Umbraco 17 site. The README covers everything above.

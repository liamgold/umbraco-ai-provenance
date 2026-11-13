# AI Provenance: design

AI Provenance is an open-source package for Umbraco. It records where AI was involved in content, one property at a time. Editors see the marks in the backoffice. Developers read them in Razor and the Content Delivery API. Sites can show an AI label next to marked content where they choose to.

This document describes the design and the reasons behind it. The package is in development, so details can change.

## Why

Readers and regulators increasingly expect websites to say when AI wrote or changed content. The EU AI Act's transparency rules apply from August 2026.

Umbraco.AI logs AI activity, such as which prompts ran and when. It does not store anything on the content itself. A developer cannot ask "did AI write this field?" and get an answer. This package adds that information.

## What it does

1. Editors mark any property on a document as **AI generated** or **AI modified**, or remove the mark.
2. When an Umbraco.AI agent changes a property, an optional add-on package marks the property automatically.
3. Editors see which properties carry a mark, in each property's action menu and in the document's Info view.
4. Developers read the marks in Razor and in the Content Delivery API output.
5. A tag helper renders an accessible AI label on the website. Sites opt in field by field.
6. It works on any document type, with no changes to the document type.

## What it does not do

- **It does not detect AI.** The package records what editors mark and what Umbraco.AI agents write. Text pasted in from another AI tool stays unmarked until an editor marks it. An unmarked property does not prove that a person wrote it.
- **It does not watermark content.** It does not embed C2PA Content Credentials or other machine-readable marks in the content itself.
- **It does not make a site compliant** with the EU AI Act or any other law. It provides information that can help with compliance.
- **It does not cover media.** For images, use [AI Image Disclosure](https://github.com/thebuilder/umbraco-ai-image-disclosure) (see [Prior art](#prior-art)).

## Concepts

| Term | Meaning |
|---|---|
| **Origin** | `AiGenerated` (AI created the content), `AiModified` (a person created the content and AI changed it), or no mark |
| **Source** | How the mark was set: `Manual` (an editor set it) or `UmbracoAI` (the package set it automatically) |
| **Provenance record** | The origin of one property, for one document, culture and segment |

The two origins match the European Commission's [icons for AI-generated and AI-modified content](https://digital-strategy.ec.europa.eu/en/policies/eu-icons-labelling-ai-generated-content).

## Design decisions

### The package stores marks in its own table

The package does not add a property to document types. Marking then works on every document type, including existing ones, with no schema changes.

The alternative was a hidden property added through a composition. That approach needs schema changes, and editors cannot easily add it to existing document types.

### Marks are per property, culture and segment

A variant document can carry different marks in each language. Invariant properties use no culture.

A property inside a Block List or Block Grid item has its own mark. The package identifies it by the block's element key and the property's alias.

### Editors see marks in two places

Umbraco 17 has no supported extension point for a badge next to a property's label. The package uses two supported places instead:

- Each property's action menu shows the current mark, with the actions to change or remove it.
- The document's Info view lists every marked property.

If Umbraco adds a way to extend the property label, a badge can follow.

### Draft and published marks are separate

Editing a property's mark changes the draft. Publishing copies the draft marks to the published state. The website and the Delivery API read the published state. A mark therefore appears on the site at the same time as the content it describes.

### A manual edit does not remove a mark

When an editor rewrites an AI-generated field, the mark stays. The editor removes the mark or changes it to AI modified. The package cannot tell how much of the AI text survived an edit, so a guess would often be wrong.

### Automatic marking covers agent writes

Umbraco.AI agents, including Automate workflows, change content on the server. The add-on package detects these saves and marks each changed property:

- A change to an **empty** property marks it `AiGenerated`.
- A change to a property **with content** marks it `AiModified`.

An `AiGenerated` mark never changes to `AiModified` automatically.

The add-on compares whole property values. When an agent changes a property inside a block, the add-on marks the block property as a whole, not the property inside the block.

When an editor applies a prompt suggestion, Umbraco.AI changes the value in the browser and fires no event. The package cannot detect this in a supported way, so editors mark these properties themselves. The extension point that would fix this is in [`discovery.md`](discovery.md#proposed-umbracoai-extension-points), as a proposal for Umbraco.AI.

### Marks follow the content

Copying a document copies its marks. Deleting a document deletes its marks.

Rolling back a document does not restore its marks. Umbraco's rollback notification does not say which version the editor chose. After a rollback, the draft keeps its current marks, and the editor adjusts them.

### Umbraco.AI integration is a separate package

- **`Umbraco.Community.AIProvenance`** has the marking, the backoffice, the Razor helpers and the Delivery API output. It does not reference Umbraco.AI, and it supports Umbraco 17.6 and later. Umbraco 17.6 added the published cultures to the publish notification, which keeps marks correct per language.
- **`Umbraco.Community.AIProvenance.UmbracoAI`** adds automatic marking. It depends on the full Umbraco.AI package, 17.8 or later, and needs the same Umbraco version as the main package. Installing it installs Umbraco.AI.

Sites without Umbraco.AI install only the first package. Changes in Umbraco.AI only affect the add-on. The add-on can also move into Umbraco.AI itself later.

### Automatic marks record the model

When the add-on marks a property, it also stores the AI provider and model that Umbraco.AI used. The first version does not display this information. It is stored now because the package cannot recover it later for content that is already marked.

## Data model

The table `aiProvenance` holds one row per marked property:

| Column | Type | Notes |
|---|---|---|
| `id` | int, identity | Primary key |
| `documentKey` | Guid | The document's key |
| `elementKey` | Guid, null | The block's element key, for a property inside a block. Null otherwise |
| `propertyAlias` | nvarchar(255) | The property's alias. Inside a block, the alias of the property in the block |
| `culture` | nvarchar(50), null | Null for invariant properties |
| `segment` | nvarchar(255), null | Null when the property has no segment |
| `state` | tinyint | 0 = draft, 1 = published |
| `origin` | tinyint | 1 = AiGenerated, 2 = AiModified |
| `source` | tinyint | 1 = Manual, 2 = UmbracoAI |
| `aiProvider` | nvarchar(100), null | Set by automatic marking only |
| `aiModel` | nvarchar(255), null | Set by automatic marking only |
| `updatedBy` | Guid | The user's key |
| `updatedUtc` | datetime | |

A unique index covers `documentKey`, `elementKey`, `propertyAlias`, `culture`, `segment` and `state`. To remove a mark, the package deletes the row.

## For developers

These APIs are planned. The names can change before the first release.

A Razor extension returns a property's origin for the current culture:

```cshtml
@if (Model.GetAiOrigin("summary") == AiOrigin.AiGenerated) { ... }
```

A tag helper renders a label when the property carries a mark. It renders only its own content when the property has no mark. Sites register it in `Views/_ViewImports.cshtml` with `@addTagHelper *, Umbraco.Community.AIProvenance`:

```cshtml
<ai-label for="summary" />
<ai-label for="summary" position="after"><p>@Model.Value("summary")</p></ai-label>
<ai-label for="text" item="@block.Content">...</ai-label>
```

The label shows "AI generated" or "AI modified", with a fuller explanation that opens on click, tap or keyboard and that screen readers announce. Sites translate the text with dictionary items (`AIProvenance.AiGenerated`, `AIProvenance.AiGeneratedDescription`, `AIProvenance.AiModified` and `AIProvenance.AiModifiedDescription`). They override the markup with their own `Views/Partials/AIProvenance/Label.cshtml`.

The package does not include the European Commission's AI icons. The Commission offers them for free use, but names no licence for them, and its legal notice excludes logos from its reuse policy. Sites can download the icons and add them to their own label view.

The Content Delivery API adds the marked properties to each content item, as a reserved entry in `properties`:

```json
"properties": {
  "summary": "...",
  "bodyText": "...",
  "aiProvenance": {
    "summary": "aiGenerated",
    "bodyText": "aiModified"
  }
}
```

The entry sits inside `properties` because the Delivery API does not accept extra fields beside them. Only marked properties appear, in the request's culture. In preview, the output shows draft marks. Umbraco's extended webhooks use the same output, so their payloads include the entry too.

The setting `AIProvenance:DeliveryApiEnabled` turns this output off. Properties inside blocks are not included, because a block's output does not know which document contains it.

## Umbraco versions

The first release targets Umbraco 17, the current Long Term Support (LTS) version. Both packages support Umbraco 17.6 and later. The add-on also needs Umbraco.AI 17.8 or later. Both NuGet dependencies stop below Umbraco 18, so NuGet does not install them on sites that they do not support.

Umbraco 18 changed how the Management API generates its OpenAPI documents. Packages with their own Management API endpoints need a separate build for 18 and later. Support for 18 and later comes after the 17 release, as a separate major version of the package.

## Prior art

Other platforms already record AI involvement in content:

- **Xperience by Kentico** (version 31.10) marks content fields as AI generated or AI modified. It sets the mark automatically when its built-in AI changes a field, and it gives developers the marks to show labels. This package follows its model closely.
- **Drupal's [AI Provenance](https://www.drupal.org/project/ai_provenance) module** records AI contribution per entity and field, and renders a transparency badge.
- **[AI Image Disclosure](https://github.com/thebuilder/umbraco-ai-image-disclosure)** does the same job for Umbraco images, using C2PA Content Credentials. This package covers document properties and leaves media to it.

## Relationship with Umbraco.AI

This is a community package. Umbraco HQ does not maintain it. The plan is to share the design with the Umbraco.AI team, and to contribute the feature, or the extension point it needs, to Umbraco.AI if they want it.

## Later ideas

These are not planned for the first release:

- A dashboard that lists all marked content, filtered by origin and document type
- A bulk action to mark several items at once
- Webhooks or notifications when a mark changes
- schema.org JSON-LD output, so crawlers can read the marks
- Mark history for every content version, so a rollback restores the marks
- Automatic marking of editor-applied suggestions, once Umbraco.AI offers a hook for it
- A badge next to the property label, once Umbraco offers a way to extend it
- A proposal to move the feature, or the extension point it needs, into Umbraco.AI

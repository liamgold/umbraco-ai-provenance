namespace Umbraco.Community.AIProvenance.UmbracoAI.Proposals;

/// <summary>
/// A value that an AI proposed for a property, which the editor may save later.
/// </summary>
/// <param name="UserKey">The backoffice user the AI proposed the value to.</param>
/// <param name="DocumentKey">The document.</param>
/// <param name="PropertyAlias">The property, or the property inside the block when <paramref name="ElementKey"/> is set.</param>
/// <param name="RootPropertyAlias">The document property that holds the value. The same as <paramref name="PropertyAlias"/> outside a block.</param>
/// <param name="ElementKey">The block's element key, for a property inside a block.</param>
/// <param name="Culture">The culture the AI edited, or null for the default.</param>
/// <param name="Segment">The segment the AI edited, or null for the default.</param>
/// <param name="Value">The proposed value as text, normalised for comparison.</param>
/// <param name="Provider">The AI provider, if known.</param>
/// <param name="Model">The AI model, if known.</param>
internal sealed record AiValueProposal(
    Guid UserKey,
    Guid DocumentKey,
    string PropertyAlias,
    string RootPropertyAlias,
    Guid? ElementKey,
    string? Culture,
    string? Segment,
    string Value,
    string? Provider,
    string? Model);

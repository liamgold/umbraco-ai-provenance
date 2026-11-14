namespace Umbraco.Community.AIProvenance.Services;

/// <summary>
/// One document in a publish operation, with the cultures whose marks to publish.
/// </summary>
/// <param name="DocumentKey">The document's key.</param>
/// <param name="Cultures">The published cultures, or null for every culture.</param>
/// <param name="UnpublishedCultures">Cultures that the same operation unpublished, if any.</param>
internal sealed record DocumentPublication(
    Guid DocumentKey,
    IEnumerable<string>? Cultures,
    IReadOnlyCollection<string>? UnpublishedCultures = null);

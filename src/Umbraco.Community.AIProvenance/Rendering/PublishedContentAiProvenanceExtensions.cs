using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Community.AIProvenance.Models;
using Umbraco.Community.AIProvenance.Rendering;

// The extensions live in Umbraco.Extensions, so they are available in Razor views without a using directive.
namespace Umbraco.Extensions;

/// <summary>
/// Reads AI marks in Razor views.
/// </summary>
public static class PublishedContentAiProvenanceExtensions
{
    /// <summary>
    /// Gets the AI origin of a document property, for the current culture unless a culture is given.
    /// </summary>
    /// <example><code>@if (Model.GetAiOrigin("summary") == AiOrigin.AiGenerated) { ... }</code></example>
    public static AiOrigin GetAiOrigin(this IPublishedContent content, string propertyAlias, string? culture = null, string? segment = null) =>
        Reader.GetOrigin(content, propertyAlias, culture, segment);

    /// <summary>
    /// Gets the AI origin of a property inside a block.
    /// </summary>
    /// <param name="element">The block's content element.</param>
    /// <param name="document">The document that contains the block, usually the current page.</param>
    /// <param name="propertyAlias">The alias of the property in the block.</param>
    /// <param name="culture">The culture, or null for the current culture.</param>
    /// <param name="segment">The segment, or null for the current segment.</param>
    public static AiOrigin GetAiOrigin(this IPublishedElement element, IPublishedContent document, string propertyAlias, string? culture = null, string? segment = null) =>
        Reader.GetOrigin(element, document, propertyAlias, culture, segment);

    private static IAiProvenanceReader Reader =>
        StaticServiceProvider.Instance.GetRequiredService<IAiProvenanceReader>();
}

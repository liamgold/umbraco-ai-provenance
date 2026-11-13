using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Community.AIProvenance.Models;

namespace Umbraco.Community.AIProvenance.Rendering;

/// <summary>
/// Reads the marks of published content, for websites and the Delivery API.
/// </summary>
/// <remarks>
/// It reads the published marks, or the draft marks in preview. For a property that varies by culture or segment,
/// it uses the given culture and segment, or the current request's.
/// </remarks>
public interface IAiProvenanceReader
{
    /// <summary>
    /// Gets the origin of a document property.
    /// </summary>
    AiOrigin GetOrigin(IPublishedContent content, string propertyAlias, string? culture = null, string? segment = null);

    /// <summary>
    /// Gets the origin of a property inside a block.
    /// </summary>
    /// <param name="element">The block's content element.</param>
    /// <param name="document">The document that contains the block.</param>
    /// <param name="propertyAlias">The alias of the property in the block.</param>
    /// <param name="culture">The culture, or null for the current request's culture.</param>
    /// <param name="segment">The segment, or null for the current request's segment.</param>
    AiOrigin GetOrigin(IPublishedElement element, IPublishedContent document, string propertyAlias, string? culture = null, string? segment = null);

    /// <summary>
    /// Gets the origins of a document's marked properties, keyed by property alias. Properties inside blocks are not included.
    /// </summary>
    IReadOnlyDictionary<string, AiOrigin> GetOrigins(IPublishedContent content, string? culture = null, string? segment = null, bool? preview = null);
}

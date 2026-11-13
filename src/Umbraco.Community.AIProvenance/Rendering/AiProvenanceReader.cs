using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Community.AIProvenance.Models;
using Umbraco.Community.AIProvenance.Services;
using Umbraco.Extensions;

namespace Umbraco.Community.AIProvenance.Rendering;

internal sealed class AiProvenanceReader : IAiProvenanceReader
{
    private readonly IAiProvenanceService _service;
    private readonly IVariationContextAccessor _variationContextAccessor;
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;
    private readonly AppCaches _appCaches;

    public AiProvenanceReader(
        IAiProvenanceService service,
        IVariationContextAccessor variationContextAccessor,
        IUmbracoContextAccessor umbracoContextAccessor,
        AppCaches appCaches)
    {
        _service = service;
        _variationContextAccessor = variationContextAccessor;
        _umbracoContextAccessor = umbracoContextAccessor;
        _appCaches = appCaches;
    }

    public AiOrigin GetOrigin(IPublishedContent content, string propertyAlias, string? culture = null, string? segment = null) =>
        GetOrigin(content, elementKey: null, content.Key, propertyAlias, culture, segment, preview: null);

    public AiOrigin GetOrigin(IPublishedElement element, IPublishedContent document, string propertyAlias, string? culture = null, string? segment = null) =>
        ReferenceEquals(element, document) || element.Key == document.Key
            ? GetOrigin(document, propertyAlias, culture, segment)
            : GetOrigin(element, element.Key, document.Key, propertyAlias, culture, segment, preview: null);

    public IReadOnlyDictionary<string, AiOrigin> GetOrigins(IPublishedContent content, string? culture = null, string? segment = null, bool? preview = null)
    {
        var origins = new Dictionary<string, AiOrigin>(StringComparer.OrdinalIgnoreCase);

        foreach (AiProvenanceMark mark in GetMarks(content.Key, preview).Where(x => x.Property.ElementKey is null))
        {
            string alias = mark.Property.PropertyAlias;
            if (origins.ContainsKey(alias))
            {
                continue;
            }

            AiOrigin origin = GetOrigin(content, elementKey: null, content.Key, alias, culture, segment, preview);
            if (origin != AiOrigin.None)
            {
                origins[content.GetProperty(alias)?.Alias ?? alias] = origin;
            }
        }

        return origins;
    }

    private AiOrigin GetOrigin(
        IPublishedElement element,
        Guid? elementKey,
        Guid documentKey,
        string propertyAlias,
        string? culture,
        string? segment,
        bool? preview)
    {
        IPublishedProperty? property = element.GetProperty(propertyAlias);
        if (property is null)
        {
            return AiOrigin.None;
        }

        // Only a property that varies by culture or segment has a culture or segment in its mark.
        VariationContext? variationContext = _variationContextAccessor.VariationContext;
        string? markCulture = property.PropertyType.Variations.VariesByCulture()
            ? AiPropertyReference.NormalizeCulture(culture ?? variationContext?.Culture)
            : null;
        string? markSegment = property.PropertyType.Variations.VariesBySegment()
            ? NullIfEmpty(segment ?? variationContext?.Segment)
            : null;

        return GetMarks(documentKey, preview)
            .FirstOrDefault(mark =>
                mark.Property.ElementKey == elementKey
                && string.Equals(mark.Property.PropertyAlias, propertyAlias, StringComparison.OrdinalIgnoreCase)
                && mark.Property.Culture == markCulture
                && mark.Property.Segment == markSegment)
            ?.Origin ?? AiOrigin.None;
    }

    /// <summary>
    /// Gets a document's marks once per request, so a page with many labels reads the database once.
    /// </summary>
    private IReadOnlyCollection<AiProvenanceMark> GetMarks(Guid documentKey, bool? preview)
    {
        AiProvenanceState state = (preview ?? IsWebsitePreview()) ? AiProvenanceState.Draft : AiProvenanceState.Published;

        return _appCaches.RequestCache.GetCacheItem(
                   $"AiProvenance.Marks.{documentKey}.{state}",
                   () => _service.GetMarksAsync(documentKey, state).GetAwaiter().GetResult())
               ?? [];
    }

    private bool IsWebsitePreview() =>
        _umbracoContextAccessor.TryGetUmbracoContext(out IUmbracoContext? umbracoContext) && umbracoContext.InPreviewMode;

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}

using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.DeliveryApi;
using Umbraco.Cms.Core.Models.DeliveryApi;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Community.AIProvenance.Models;
using Umbraco.Community.AIProvenance.Rendering;

namespace Umbraco.Community.AIProvenance.DeliveryApi;

/// <summary>
/// Adds an <c>aiProvenance</c> entry to each content item's <c>properties</c> in the Delivery API,
/// with the origin of each marked property.
/// </summary>
/// <remarks>
/// The entry sits inside <c>properties</c>, because the Delivery API's serializer only accepts its own response type.
/// </remarks>
internal sealed class AiProvenanceApiContentResponseBuilder : ApiContentResponseBuilder
{
    /// <summary>The key of the entry in <c>properties</c>.</summary>
    public const string PropertyName = "aiProvenance";

    private readonly IAiProvenanceReader _reader;
    private readonly IRequestPreviewService _requestPreviewService;
    private readonly IOptionsMonitor<AiProvenanceSettings> _settings;

    public AiProvenanceApiContentResponseBuilder(
        IApiContentNameProvider apiContentNameProvider,
        IApiContentRouteBuilder apiContentRouteBuilder,
        IOutputExpansionStrategyAccessor outputExpansionStrategyAccessor,
        IVariationContextAccessor variationContextAccessor,
        IAiProvenanceReader reader,
        IRequestPreviewService requestPreviewService,
        IOptionsMonitor<AiProvenanceSettings> settings)
        : base(apiContentNameProvider, apiContentRouteBuilder, outputExpansionStrategyAccessor, variationContextAccessor)
    {
        _reader = reader;
        _requestPreviewService = requestPreviewService;
        _settings = settings;
    }

    protected override IApiContentResponse Create(IPublishedContent content, string name, IApiContentRoute route, IDictionary<string, object?> properties)
    {
        if (_settings.CurrentValue.DeliveryApiEnabled)
        {
            IReadOnlyDictionary<string, AiOrigin> origins = _reader.GetOrigins(content, preview: _requestPreviewService.IsPreview());
            if (origins.Count > 0)
            {
                properties[PropertyName] = origins.ToDictionary(x => x.Key, x => ToApiValue(x.Value));
            }
        }

        return base.Create(content, name, route, properties);
    }

    private static string ToApiValue(AiOrigin origin) => origin == AiOrigin.AiGenerated ? "aiGenerated" : "aiModified";
}

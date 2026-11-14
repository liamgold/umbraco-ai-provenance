using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Api.Delivery.Controllers.Content;
using Umbraco.Cms.Core.DeliveryApi;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Community.AIProvenance.Models;
using Umbraco.Community.AIProvenance.Rendering;

namespace Umbraco.Community.AIProvenance.DeliveryApi;

/// <summary>
/// Gets the marked properties of published content through the Content Delivery API.
/// </summary>
/// <remarks>
/// The controller inherits Umbraco's content item controller, so the endpoints work like Umbraco's own content
/// endpoints: they need the Delivery API to be enabled, honour the API key, the <c>Accept-Language</c>,
/// <c>Accept-Segment</c>, <c>Preview</c> and <c>Start-Item</c> headers, check member access to protected content,
/// and share the content output cache. The routes sit under Umbraco's <c>content</c> route, beside the item they describe.
/// </remarks>
[ApiVersion("2.0")]
[ApiExplorerSettings(GroupName = "AI Provenance")]
public sealed class AiProvenanceDeliveryApiController : ContentApiItemControllerBase
{
    private readonly IRequestMemberAccessService _requestMemberAccessService;
    private readonly IRequestPreviewService _requestPreviewService;
    private readonly IAiProvenanceReader _reader;
    private readonly IOptionsMonitor<AiProvenanceSettings> _settings;
    private readonly IApiContentRouteBuilder _apiContentRouteBuilder;
    private readonly IVariationContextAccessor _variationContextAccessor;

    public AiProvenanceDeliveryApiController(
        IApiPublishedContentCache apiPublishedContentCache,
        IApiContentResponseBuilder apiContentResponseBuilder,
        IRequestMemberAccessService requestMemberAccessService,
        IRequestPreviewService requestPreviewService,
        IAiProvenanceReader reader,
        IOptionsMonitor<AiProvenanceSettings> settings,
        IApiContentRouteBuilder apiContentRouteBuilder,
        IVariationContextAccessor variationContextAccessor)
        : base(apiPublishedContentCache, apiContentResponseBuilder)
    {
        _requestMemberAccessService = requestMemberAccessService;
        _requestPreviewService = requestPreviewService;
        _reader = reader;
        _settings = settings;
        _apiContentRouteBuilder = apiContentRouteBuilder;
        _variationContextAccessor = variationContextAccessor;
    }

    /// <summary>
    /// Gets the marked properties of a content item, in the request's culture and segment.
    /// </summary>
    /// <param name="id">The content item's key.</param>
    [HttpGet("item/{id:guid}/ai-provenance")]
    [MapToApiVersion("2.0")]
    [ProducesResponseType<AiProvenanceResponseModel>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ById(Guid id)
    {
        if (_settings.CurrentValue.DeliveryApiEnabled is false)
        {
            return NotFound();
        }

        IPublishedContent? content = await ApiPublishedContentCache.GetByIdAsync(id).ConfigureAwait(false);
        if (content is null || IsAvailable(content) is false)
        {
            return NotFound();
        }

        IActionResult? deniedAccessResult = await HandleMemberAccessAsync(content, _requestMemberAccessService).ConfigureAwait(false);
        if (deniedAccessResult is not null)
        {
            return deniedAccessResult;
        }

        SetOutputCacheContent(content);
        return Ok(CreateResponse(content));
    }

    /// <summary>
    /// Gets the marked properties of several content items, in the request's culture and segment.
    /// Keys that match no item published in that culture are left out.
    /// </summary>
    /// <param name="ids">The content items' keys.</param>
    [HttpGet("items/ai-provenance")]
    [MapToApiVersion("2.0")]
    [ProducesResponseType<IEnumerable<AiProvenanceResponseModel>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ByIds([FromQuery(Name = "id")] HashSet<Guid> ids)
    {
        if (_settings.CurrentValue.DeliveryApiEnabled is false)
        {
            return NotFound();
        }

        IPublishedContent[] contentItems = (await ApiPublishedContentCache.GetByIdsAsync(ids).ConfigureAwait(false)).ToArray();

        IActionResult? deniedAccessResult = await HandleMemberAccessAsync(contentItems, _requestMemberAccessService).ConfigureAwait(false);
        if (deniedAccessResult is not null)
        {
            return deniedAccessResult;
        }

        SetOutputCacheContent(contentItems);
        return Ok(contentItems.Where(IsAvailable).Select(CreateResponse).ToArray());
    }

    /// <summary>
    /// Whether the content is available in the request's culture. Umbraco's own content endpoints leave out content
    /// they can't build a route for, such as a culture that isn't published, so these endpoints do the same.
    /// </summary>
    private bool IsAvailable(IPublishedContent content) =>
        _apiContentRouteBuilder.Build(content, _variationContextAccessor.VariationContext?.Culture) is not null;

    private AiProvenanceResponseModel CreateResponse(IPublishedContent content)
    {
        // The culture and segment come from the variation context, which Umbraco sets from the request headers.
        IReadOnlyDictionary<string, AiOrigin> origins = _reader.GetOrigins(content, preview: _requestPreviewService.IsPreview());
        return new AiProvenanceResponseModel
        {
            Id = content.Key,
            Properties = origins.ToDictionary(x => x.Key, x => ToApiValue(x.Value)),
        };
    }

    private static string ToApiValue(AiOrigin origin) => origin == AiOrigin.AiGenerated ? "aiGenerated" : "aiModified";
}

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Api.Common.Filters;
using Umbraco.Cms.Core.Actions;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Security.Authorization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Cms.Web.Common.Routing;
using Umbraco.Community.AIProvenance.Api.Models;
using Umbraco.Community.AIProvenance.Models;
using Umbraco.Community.AIProvenance.Services;
using Umbraco.Extensions;

namespace Umbraco.Community.AIProvenance.Api;

/// <summary>
/// Reads and changes a document's draft marks from the backoffice.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Document marks")]
[BackOfficeRoute("umbracocommunityaiprovenance/api/v{version:apiVersion}/document")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessContent)]
[MapToApi(Constants.ApiName)]
[JsonOptionsName(Umbraco.Cms.Core.Constants.JsonOptionsNames.BackOffice)]
public sealed class DocumentMarksController : ControllerBase
{
    private readonly IAiProvenanceService _provenanceService;
    private readonly IContentService _contentService;
    private readonly IAuthorizationService _authorizationService;
    private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;

    public DocumentMarksController(
        IAiProvenanceService provenanceService,
        IContentService contentService,
        IAuthorizationService authorizationService,
        IBackOfficeSecurityAccessor backOfficeSecurityAccessor)
    {
        _provenanceService = provenanceService;
        _contentService = contentService;
        _authorizationService = authorizationService;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
    }

    /// <summary>
    /// Gets every draft mark of a document.
    /// </summary>
    [HttpGet("{id:guid}/marks")]
    [ProducesResponseType<IEnumerable<MarkResponseModel>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDocumentMarks(Guid id)
    {
        if (await IsAuthorizedAsync(ActionBrowse.ActionLetter, id) is false)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        if (_contentService.GetById(id) is null)
        {
            return NotFound();
        }

        IReadOnlyCollection<AiProvenanceMark> marks = await _provenanceService.GetMarksAsync(id, AiProvenanceState.Draft);
        return Ok(marks.Select(mark => new MarkResponseModel
        {
            PropertyAlias = mark.Property.PropertyAlias,
            Culture = mark.Property.Culture,
            Segment = mark.Property.Segment,
            ElementKey = mark.Property.ElementKey,
            Origin = mark.Origin,
            Source = mark.Source,
            AiProvider = mark.AiProvider,
            AiModel = mark.AiModel,
            UpdatedUtc = mark.UpdatedUtc,
        }));
    }

    /// <summary>
    /// Sets or removes the draft mark of one property value. It needs the same permission as updating the document,
    /// and, for a value in a culture, access to that culture's language.
    /// </summary>
    [HttpPut("{id:guid}/marks")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetDocumentMark(Guid id, SetMarkRequestModel request)
    {
        IEnumerable<string> cultures = string.IsNullOrWhiteSpace(request.Culture) ? [] : [request.Culture];
        if (await IsAuthorizedAsync(ActionUpdate.ActionLetter, id, cultures) is false)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        if (_contentService.GetById(id) is null)
        {
            return NotFound();
        }

        Guid userKey = _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser?.Key
            ?? throw new InvalidOperationException("No backoffice user is signed in.");

        var property = new AiPropertyReference(id, request.PropertyAlias, request.Culture, request.Segment, request.ElementKey);
        await _provenanceService.SetMarkAsync(property, request.Origin, AiProvenanceSource.Manual, userKey);
        return Ok();
    }

    /// <summary>
    /// Checks the user's permission on the document. When cultures are given, Umbraco also checks that the user
    /// has access to those languages.
    /// </summary>
    private async Task<bool> IsAuthorizedAsync(string permission, Guid documentKey, IEnumerable<string>? cultures = null)
    {
        string[] cultureList = cultures?.ToArray() ?? [];
        AuthorizationResult result = await _authorizationService.AuthorizeResourceAsync(
            User,
            cultureList.Length > 0
                ? ContentPermissionResource.WithKeys(permission, documentKey, cultureList)
                : ContentPermissionResource.WithKeys(permission, documentKey),
            AuthorizationPolicies.ContentPermissionByResource);
        return result.Succeeded;
    }
}

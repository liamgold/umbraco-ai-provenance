using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Api.Common.Filters;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Cms.Web.Common.Routing;
using Umbraco.Community.AIProvenance.Api.Models;

namespace Umbraco.Community.AIProvenance.Api;

/// <summary>
/// Gives the backoffice the package settings it needs.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Configuration")]
[BackOfficeRoute("umbracocommunityaiprovenance/api/v{version:apiVersion}/configuration")]
[Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
[MapToApi(Constants.ApiName)]
[JsonOptionsName(Umbraco.Cms.Core.Constants.JsonOptionsNames.BackOffice)]
public sealed class ConfigurationController : ControllerBase
{
    private readonly IOptionsMonitor<AiProvenanceSettings> _settings;

    public ConfigurationController(IOptionsMonitor<AiProvenanceSettings> settings) => _settings = settings;

    /// <summary>
    /// Gets the package settings for the backoffice.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<ConfigurationResponseModel>(StatusCodes.Status200OK)]
    public IActionResult GetConfiguration() =>
        Ok(new ConfigurationResponseModel
        {
            AdditionalPropertyEditorUis = _settings.CurrentValue.AdditionalPropertyEditorUis
                .Where(alias => string.IsNullOrWhiteSpace(alias) is false)
                .Select(alias => alias.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
        });
}

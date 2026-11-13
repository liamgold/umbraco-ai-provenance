namespace Umbraco.Community.AIProvenance.Api.Models;

/// <summary>
/// The package settings that the backoffice needs.
/// </summary>
public sealed class ConfigurationResponseModel
{
    /// <summary>Property editor UI aliases that get the mark actions, on top of Umbraco's own content editors.</summary>
    public required IEnumerable<string> AdditionalPropertyEditorUis { get; init; }
}

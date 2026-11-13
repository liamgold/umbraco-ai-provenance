namespace Umbraco.Community.AIProvenance.UmbracoAI;

/// <summary>
/// The add-on's settings, in the <c>AIProvenance:UmbracoAI</c> section of appsettings.json.
/// </summary>
public sealed class AiProvenanceUmbracoAISettings
{
    /// <summary>The name of the configuration section.</summary>
    public const string SectionName = "AIProvenance:UmbracoAI";

    /// <summary>Whether to mark properties when Umbraco.AI agents change them. On by default.</summary>
    public bool AutomaticMarking { get; set; } = true;
}

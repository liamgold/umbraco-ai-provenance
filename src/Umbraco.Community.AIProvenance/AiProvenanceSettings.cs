namespace Umbraco.Community.AIProvenance;

/// <summary>
/// The package's settings, in the <c>AIProvenance</c> section of appsettings.json.
/// </summary>
public sealed class AiProvenanceSettings
{
    /// <summary>The name of the configuration section.</summary>
    public const string SectionName = "AIProvenance";

    /// <summary>Whether the Delivery API output includes the <c>aiProvenance</c> entry. On by default.</summary>
    public bool DeliveryApiEnabled { get; set; } = true;
}

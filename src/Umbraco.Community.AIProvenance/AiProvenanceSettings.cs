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

    /// <summary>
    /// Property editor UI aliases that also get the mark actions, on top of Umbraco's own content editors.
    /// For example <c>My.PropertyEditorUi.Headline</c> for a third-party editor.
    /// </summary>
    public string[] AdditionalPropertyEditorUis { get; set; } = [];
}

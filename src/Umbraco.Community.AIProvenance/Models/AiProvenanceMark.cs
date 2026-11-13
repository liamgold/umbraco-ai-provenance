namespace Umbraco.Community.AIProvenance.Models;

/// <summary>
/// The AI origin of one property value, in one state of a document.
/// </summary>
public sealed record AiProvenanceMark
{
    /// <summary>The property value that the mark belongs to.</summary>
    public required AiPropertyReference Property { get; init; }

    /// <summary>Whether the mark belongs to the draft or the published content.</summary>
    public required AiProvenanceState State { get; init; }

    /// <summary>How AI was involved.</summary>
    public required AiOrigin Origin { get; init; }

    /// <summary>What set the mark.</summary>
    public required AiProvenanceSource Source { get; init; }

    /// <summary>The AI provider, when the Umbraco.AI add-on set the mark.</summary>
    public string? AiProvider { get; init; }

    /// <summary>The AI model, when the Umbraco.AI add-on set the mark.</summary>
    public string? AiModel { get; init; }

    /// <summary>The key of the user who set the mark.</summary>
    public required Guid UpdatedBy { get; init; }

    /// <summary>When the mark was set, in UTC.</summary>
    public required DateTime UpdatedUtc { get; init; }
}

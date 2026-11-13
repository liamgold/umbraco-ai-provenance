namespace Umbraco.Community.AIProvenance.Models;

/// <summary>
/// How AI was involved in a property's content.
/// </summary>
public enum AiOrigin : byte
{
    /// <summary>The property has no mark.</summary>
    None = 0,

    /// <summary>AI created the content.</summary>
    AiGenerated = 1,

    /// <summary>A person created the content, and AI changed it.</summary>
    AiModified = 2,
}

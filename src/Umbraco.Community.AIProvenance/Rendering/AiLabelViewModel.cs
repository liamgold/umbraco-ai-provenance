using Umbraco.Community.AIProvenance.Models;

namespace Umbraco.Community.AIProvenance.Rendering;

/// <summary>
/// The model of the AI label partial view, <c>Views/Partials/AIProvenance/Label.cshtml</c>.
/// </summary>
public sealed class AiLabelViewModel
{
    /// <summary>How AI was involved. Never <see cref="AiOrigin.None"/>, because unmarked properties get no label.</summary>
    public required AiOrigin Origin { get; init; }

    /// <summary>The short label, such as "AI generated".</summary>
    public required string Text { get; init; }

    /// <summary>The fuller explanation, such as "AI generated this content.".</summary>
    public required string Description { get; init; }

    /// <summary>The alias of the property that the label belongs to.</summary>
    public required string PropertyAlias { get; init; }
}

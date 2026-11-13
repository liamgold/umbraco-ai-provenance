using Umbraco.Community.AIProvenance.Models;

namespace Umbraco.Community.AIProvenance.Api.Models;

/// <summary>
/// One draft mark of a document, for the backoffice.
/// </summary>
public sealed class MarkResponseModel
{
    public required string PropertyAlias { get; init; }

    public string? Culture { get; init; }

    public string? Segment { get; init; }

    /// <summary>The block's element key, for a property inside a block.</summary>
    public Guid? ElementKey { get; init; }

    public required AiOrigin Origin { get; init; }

    public required AiProvenanceSource Source { get; init; }

    public string? AiProvider { get; init; }

    public string? AiModel { get; init; }

    public required DateTime UpdatedUtc { get; init; }
}

using System.ComponentModel.DataAnnotations;
using Umbraco.Community.AIProvenance.Models;

namespace Umbraco.Community.AIProvenance.Api.Models;

/// <summary>
/// Sets or removes the draft mark of one property value.
/// </summary>
public sealed class SetMarkRequestModel
{
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public required string PropertyAlias { get; init; }

    /// <summary>The culture, or null for an invariant property.</summary>
    [StringLength(50)]
    public string? Culture { get; init; }

    /// <summary>The segment, or null when the value has no segment.</summary>
    [StringLength(255)]
    public string? Segment { get; init; }

    /// <summary>The block's element key, for a property inside a block.</summary>
    public Guid? ElementKey { get; init; }

    /// <summary>The new origin. <see cref="AiOrigin.None"/> removes the mark.</summary>
    [EnumDataType(typeof(AiOrigin))]
    public required AiOrigin Origin { get; init; }
}

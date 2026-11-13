namespace Umbraco.Community.AIProvenance.Models;

/// <summary>
/// Identifies one property value of a document: a property, in one culture and segment.
/// </summary>
/// <param name="DocumentKey">The document's key.</param>
/// <param name="PropertyAlias">The property's alias. Inside a block, the alias of the property in the block.</param>
/// <param name="Culture">The culture, or null for an invariant property.</param>
/// <param name="Segment">The segment, or null when the value has no segment.</param>
/// <param name="ElementKey">The block's element key for a property inside a block, or null.</param>
public sealed record AiPropertyReference(
    Guid DocumentKey,
    string PropertyAlias,
    string? Culture = null,
    string? Segment = null,
    Guid? ElementKey = null)
{
    /// <summary>
    /// Returns a copy with an empty culture or segment turned into null, and the culture in lower case,
    /// so that lookups match whatever casing the caller used.
    /// </summary>
    public AiPropertyReference Normalize() => this with
    {
        Culture = NormalizeCulture(Culture),
        Segment = string.IsNullOrWhiteSpace(Segment) ? null : Segment,
    };

    /// <summary>
    /// Turns an empty or wildcard culture into null, and any other culture into lower case.
    /// </summary>
    public static string? NormalizeCulture(string? culture) =>
        string.IsNullOrWhiteSpace(culture) || culture == "*" ? null : culture.ToLowerInvariant();
}

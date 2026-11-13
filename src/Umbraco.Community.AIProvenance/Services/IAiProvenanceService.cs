using Umbraco.Community.AIProvenance.Models;

namespace Umbraco.Community.AIProvenance.Services;

/// <summary>
/// Reads and changes the AI marks on documents.
/// </summary>
/// <remarks>
/// Editors and the Umbraco.AI add-on change the draft marks. Publishing copies them to the published state,
/// which the website and the Delivery API read.
/// </remarks>
public interface IAiProvenanceService
{
    /// <summary>
    /// Gets every mark of a document in one state, for all cultures, segments and blocks.
    /// </summary>
    Task<IReadOnlyCollection<AiProvenanceMark>> GetMarksAsync(Guid documentKey, AiProvenanceState state);

    /// <summary>
    /// Gets the mark of one property value, or null when it has no mark.
    /// </summary>
    /// <remarks>
    /// The culture and segment must match exactly. Pass a null culture for an invariant property.
    /// </remarks>
    Task<AiProvenanceMark?> GetMarkAsync(AiPropertyReference property, AiProvenanceState state);

    /// <summary>
    /// Sets the draft mark of one property value. <see cref="AiOrigin.None"/> removes the mark.
    /// </summary>
    Task SetMarkAsync(
        AiPropertyReference property,
        AiOrigin origin,
        AiProvenanceSource source,
        Guid userKey,
        string? aiProvider = null,
        string? aiModel = null);

    /// <summary>
    /// Removes the draft mark of one property value.
    /// </summary>
    Task RemoveMarkAsync(AiPropertyReference property);

    /// <summary>
    /// Replaces the published marks with the draft marks, for the given cultures and for invariant properties.
    /// </summary>
    /// <param name="documentKey">The document's key.</param>
    /// <param name="cultures">The published cultures, or null to publish every culture.</param>
    Task PublishAsync(Guid documentKey, IEnumerable<string>? cultures);

    /// <summary>
    /// Removes published marks.
    /// </summary>
    /// <param name="documentKey">The document's key.</param>
    /// <param name="cultures">The unpublished cultures, or null when the whole document is unpublished.</param>
    Task UnpublishAsync(Guid documentKey, IEnumerable<string>? cultures);

    /// <summary>
    /// Copies the draft marks of one document to another, replacing the target's draft marks.
    /// </summary>
    /// <remarks>
    /// Marks on properties inside blocks are not copied. Umbraco gives every block a new key when it copies a document.
    /// </remarks>
    Task CopyAsync(Guid sourceDocumentKey, Guid targetDocumentKey);

    /// <summary>
    /// Deletes every mark of a document, draft and published.
    /// </summary>
    Task DeleteAllAsync(Guid documentKey);
}

namespace Umbraco.Community.AIProvenance.DeliveryApi;

/// <summary>
/// The marked properties of one content item in the Content Delivery API.
/// </summary>
public sealed class AiProvenanceResponseModel
{
    /// <summary>The content item's key.</summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// The origin of each marked property, by property alias: <c>aiGenerated</c> or <c>aiModified</c>.
    /// Unmarked properties are left out.
    /// </summary>
    public required IReadOnlyDictionary<string, string> Properties { get; init; }
}

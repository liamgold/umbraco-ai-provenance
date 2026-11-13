namespace Umbraco.Community.AIProvenance.Models;

/// <summary>
/// Which state of a document a mark belongs to.
/// </summary>
public enum AiProvenanceState : byte
{
    /// <summary>The draft that editors work on.</summary>
    Draft = 0,

    /// <summary>The published content that the website and the Delivery API read.</summary>
    Published = 1,
}

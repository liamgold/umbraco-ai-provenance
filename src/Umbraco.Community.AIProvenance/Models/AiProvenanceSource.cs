namespace Umbraco.Community.AIProvenance.Models;

/// <summary>
/// What set a mark.
/// </summary>
public enum AiProvenanceSource : byte
{
    /// <summary>An editor set the mark in the backoffice.</summary>
    Manual = 1,

    /// <summary>The Umbraco.AI add-on set the mark automatically.</summary>
    UmbracoAI = 2,
}

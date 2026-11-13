using Umbraco.Cms.Core.Notifications;
using Umbraco.Community.AIProvenance.Models;

namespace Umbraco.Community.AIProvenance.Notifications;

/// <summary>
/// Published after a draft mark is removed.
/// </summary>
/// <remarks>
/// It is published when the database transaction completes. It is not published when publishing, unpublishing,
/// copying or deleting a document changes marks, because those follow content operations that Umbraco already
/// raises notifications for.
/// </remarks>
public sealed class AiProvenanceMarkRemovedNotification : StatefulNotification
{
    public AiProvenanceMarkRemovedNotification(AiProvenanceMark mark) => Mark = mark;

    /// <summary>The mark that was removed.</summary>
    public AiProvenanceMark Mark { get; }
}

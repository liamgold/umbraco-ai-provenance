using Umbraco.Cms.Core.Notifications;
using Umbraco.Community.AIProvenance.Models;

namespace Umbraco.Community.AIProvenance.Notifications;

/// <summary>
/// Published after a draft mark is set or changed, by an editor or by the Umbraco.AI add-on.
/// </summary>
/// <remarks>
/// Handle it like any Umbraco notification, for example to log changes or alert a reviewer:
/// <c>builder.AddNotificationAsyncHandler&lt;AiProvenanceMarkSavedNotification, MyHandler&gt;()</c>.
/// It is published when the database transaction completes, so a handler never sees a change that was rolled back.
/// </remarks>
public sealed class AiProvenanceMarkSavedNotification : INotification
{
    public AiProvenanceMarkSavedNotification(AiProvenanceMark mark, AiOrigin previousOrigin)
    {
        Mark = mark;
        PreviousOrigin = previousOrigin;
    }

    /// <summary>The mark as it is now, with the property, origin, source, provider, model and user.</summary>
    public AiProvenanceMark Mark { get; }

    /// <summary>The origin before the change, or <see cref="AiOrigin.None"/> when the property had no mark.</summary>
    public AiOrigin PreviousOrigin { get; }
}

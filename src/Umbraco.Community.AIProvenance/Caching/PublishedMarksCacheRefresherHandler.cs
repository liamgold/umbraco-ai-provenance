using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services.Changes;
using Umbraco.Cms.Core.Sync;
using Umbraco.Extensions;

namespace Umbraco.Community.AIProvenance.Caching;

/// <summary>
/// Clears cached published marks when Umbraco refreshes its content cache.
/// </summary>
/// <remarks>
/// Umbraco raises this notification on every server after content changes, so load-balanced sites stay in step.
/// A change to a whole branch, or to all content, clears every document's marks.
/// </remarks>
internal sealed class PublishedMarksCacheRefresherHandler : INotificationHandler<ContentCacheRefresherNotification>
{
    private readonly PublishedMarksCache _cache;

    public PublishedMarksCacheRefresherHandler(PublishedMarksCache cache) => _cache = cache;

    public void Handle(ContentCacheRefresherNotification notification)
    {
        if (notification.MessageType != MessageType.RefreshByPayload
            || notification.MessageObject is not ContentCacheRefresher.JsonPayload[] payloads)
        {
            _cache.ClearAll();
            return;
        }

        foreach (ContentCacheRefresher.JsonPayload payload in payloads)
        {
            if (payload.Blueprint)
            {
                continue;
            }

            if (payload.Key is null
                || payload.ChangeTypes.HasType(TreeChangeTypes.RefreshAll)
                || payload.ChangeTypes.HasType(TreeChangeTypes.RefreshBranch))
            {
                _cache.ClearAll();
                return;
            }

            _cache.Clear(payload.Key.Value);
        }
    }
}

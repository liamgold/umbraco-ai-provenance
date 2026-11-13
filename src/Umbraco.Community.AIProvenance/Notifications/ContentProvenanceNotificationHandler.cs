using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Community.AIProvenance.Services;
using Umbraco.Extensions;

namespace Umbraco.Community.AIProvenance.Notifications;

/// <summary>
/// Keeps the marks in step with the content when documents are published, unpublished, copied or deleted.
/// </summary>
internal sealed class ContentProvenanceNotificationHandler :
    INotificationAsyncHandler<ContentPublishedNotification>,
    INotificationAsyncHandler<ContentUnpublishedNotification>,
    INotificationAsyncHandler<ContentCopiedNotification>,
    INotificationAsyncHandler<ContentDeletedNotification>
{
    private readonly IAiProvenanceService _service;

    public ContentProvenanceNotificationHandler(IAiProvenanceService service) => _service = service;

    public async Task HandleAsync(ContentPublishedNotification notification, CancellationToken cancellationToken)
    {
        foreach (IContent content in notification.PublishedEntities)
        {
            await _service.PublishAsync(content.Key, GetPublishedCultures(notification, content));

            // Unpublishing one culture of a variant document arrives as a publish.
            if (notification.UnpublishedCultures?.TryGetValue(content.Key, out IReadOnlyCollection<string>? unpublished) is true
                && unpublished.Count > 0)
            {
                await _service.UnpublishAsync(content.Key, unpublished);
            }
        }
    }

    public async Task HandleAsync(ContentUnpublishedNotification notification, CancellationToken cancellationToken)
    {
        // This notification is only raised when the whole document is unpublished.
        foreach (IContent content in notification.UnpublishedEntities)
        {
            await _service.UnpublishAsync(content.Key, cultures: null);
        }
    }

    public Task HandleAsync(ContentCopiedNotification notification, CancellationToken cancellationToken) =>
        _service.CopyAsync(notification.Original.Key, notification.Copy.Key);

    public async Task HandleAsync(ContentDeletedNotification notification, CancellationToken cancellationToken)
    {
        foreach (IContent content in notification.DeletedEntities)
        {
            await _service.DeleteAllAsync(content.Key);
        }
    }

    /// <summary>
    /// Returns the cultures to publish marks for, or null for every culture.
    /// </summary>
    private static IEnumerable<string>? GetPublishedCultures(ContentPublishedNotification notification, IContent content)
    {
        if (content.ContentType.VariesByCulture() is false)
        {
            return null;
        }

        if (notification.PublishedCultures?.TryGetValue(content.Key, out IReadOnlyCollection<string>? cultures) is true)
        {
            return cultures;
        }

        // Umbraco leaves out descendants that it re-published with an ancestor. Use the cultures that are now published.
        return content.PublishedCultures.ToArray();
    }
}

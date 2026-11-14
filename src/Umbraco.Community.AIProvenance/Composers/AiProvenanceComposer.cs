using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Community.AIProvenance.Caching;
using Umbraco.Community.AIProvenance.Notifications;
using Umbraco.Community.AIProvenance.Persistence;
using Umbraco.Community.AIProvenance.Rendering;
using Umbraco.Community.AIProvenance.Services;
using Umbraco.Extensions;

namespace Umbraco.Community.AIProvenance.Composers;

/// <summary>
/// Registers the package's services and notification handlers.
/// </summary>
public sealed class AiProvenanceComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.Configure<AiProvenanceSettings>(builder.Config.GetSection(AiProvenanceSettings.SectionName));

        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<AiProvenanceRepository>();
        builder.Services.AddSingleton<AiProvenanceService>();
        builder.Services.AddSingleton<IAiProvenanceService>(services => services.GetRequiredService<AiProvenanceService>());
        builder.Services.AddSingleton<IAiProvenanceReader, AiProvenanceReader>();
        builder.Services.AddSingleton<PublishedMarksCache>();

        builder
            .AddNotificationAsyncHandler<ContentPublishedNotification, ContentProvenanceNotificationHandler>()
            .AddNotificationAsyncHandler<ContentUnpublishedNotification, ContentProvenanceNotificationHandler>()
            .AddNotificationAsyncHandler<ContentCopiedNotification, ContentProvenanceNotificationHandler>()
            .AddNotificationAsyncHandler<ContentDeletedNotification, ContentProvenanceNotificationHandler>()
            .AddNotificationHandler<ContentCacheRefresherNotification, PublishedMarksCacheRefresherHandler>();
    }
}

using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Community.AIProvenance.Composers;

namespace Umbraco.Community.AIProvenance.UmbracoAI;

/// <summary>
/// Registers automatic marking for Umbraco.AI agent writes.
/// </summary>
[ComposeAfter(typeof(AiProvenanceComposer))]
public sealed class AiProvenanceUmbracoAIComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.Configure<AiProvenanceUmbracoAISettings>(
            builder.Config.GetSection(AiProvenanceUmbracoAISettings.SectionName));

        builder
            .AddNotificationAsyncHandler<ContentSavingNotification, AgentContentWriteNotificationHandler>()
            .AddNotificationAsyncHandler<ContentSavedNotification, AgentContentWriteNotificationHandler>();
    }
}

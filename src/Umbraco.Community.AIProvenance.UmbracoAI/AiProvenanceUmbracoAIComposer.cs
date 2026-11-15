using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.AI.Extensions;
using Umbraco.Community.AIProvenance.Composers;
using Umbraco.Community.AIProvenance.UmbracoAI.Proposals;

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

        // Remembers the values the Copilot proposes in the browser, so the editor's save can be matched to them.
        builder.Services.AddSingleton<AiValueProposalStore>();
        builder.AIChatMiddleware().Append<SetValueProposalChatMiddleware>();
    }
}

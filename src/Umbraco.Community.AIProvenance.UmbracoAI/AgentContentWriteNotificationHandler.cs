using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Community.AIProvenance.Models;
using Umbraco.Community.AIProvenance.Services;
using Umbraco.Community.AIProvenance.UmbracoAI.Proposals;
using Umbraco.Extensions;
using UmbracoAIConstants = Umbraco.AI.Core.Constants;

namespace Umbraco.Community.AIProvenance.UmbracoAI;

/// <summary>
/// Marks the properties that Umbraco.AI changes: directly through an agent's server tools, or through a value the
/// Copilot proposed in the browser that the editor then saves.
/// </summary>
/// <remarks>
/// Umbraco.AI's content tools save through Umbraco's content services in the same async flow as the agent run,
/// so the AI runtime context is active while the content saves. Every changed value in that save is an AI write.
/// A save without that context is the editor's own, unless a changed value matches a value the Copilot proposed
/// (see <see cref="SetValueProposalChatMiddleware"/>). The handler works out the changes before the save, and
/// sets the marks after the save succeeds.
/// </remarks>
internal sealed class AgentContentWriteNotificationHandler :
    INotificationAsyncHandler<ContentSavingNotification>,
    INotificationAsyncHandler<ContentSavedNotification>
{
    private const string PendingChangesKey = "AiProvenance.PendingAgentChanges";

    private readonly IAIRuntimeContextAccessor _runtimeContextAccessor;
    private readonly IContentService _contentService;
    private readonly IAiProvenanceService _provenanceService;
    private readonly IUserIdKeyResolver _userIdKeyResolver;
    private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;
    private readonly AiValueProposalStore _proposals;
    private readonly IOptionsMonitor<AiProvenanceUmbracoAISettings> _settings;
    private readonly ILogger<AgentContentWriteNotificationHandler> _logger;

    public AgentContentWriteNotificationHandler(
        IAIRuntimeContextAccessor runtimeContextAccessor,
        IContentService contentService,
        IAiProvenanceService provenanceService,
        IUserIdKeyResolver userIdKeyResolver,
        IBackOfficeSecurityAccessor backOfficeSecurityAccessor,
        AiValueProposalStore proposals,
        IOptionsMonitor<AiProvenanceUmbracoAISettings> settings,
        ILogger<AgentContentWriteNotificationHandler> logger)
    {
        _runtimeContextAccessor = runtimeContextAccessor;
        _contentService = contentService;
        _provenanceService = provenanceService;
        _userIdKeyResolver = userIdKeyResolver;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
        _proposals = proposals;
        _settings = settings;
        _logger = logger;
    }

    public Task HandleAsync(ContentSavingNotification notification, CancellationToken cancellationToken)
    {
        if (_settings.CurrentValue.AutomaticMarking is false)
        {
            return Task.CompletedTask;
        }

        AIRuntimeContext? runtimeContext = _runtimeContextAccessor.Context;
        if (runtimeContext is null)
        {
            notification.State[PendingChangesKey] = FindProposedChanges(notification);
            return Task.CompletedTask;
        }

        string? provider = runtimeContext.GetValue<object>(UmbracoAIConstants.ContextKeys.ProviderId)?.ToString();
        string? model = runtimeContext.GetValue<object>(UmbracoAIConstants.ContextKeys.ModelId)?.ToString();

        var changes = new List<PendingChange>();
        foreach (IContent content in notification.SavedEntities)
        {
            // The saved version in the database is the "before" state. New content has none.
            IContent? existing = content.HasIdentity ? _contentService.GetById(content.Key) : null;
            changes.AddRange(FindChanges(content, existing).Select(change => change with { Provider = provider, Model = model }));
        }

        notification.State[PendingChangesKey] = changes;
        return Task.CompletedTask;
    }

    public async Task HandleAsync(ContentSavedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.State.TryGetValue(PendingChangesKey, out object? value) is false || value is not List<PendingChange> changes)
        {
            return;
        }

        foreach (PendingChange change in changes)
        {
            IContent? content = notification.SavedEntities.FirstOrDefault(x => x.Key == change.Property.DocumentKey);
            Guid userKey = content is null ? Guid.Empty : await GetUserKeyAsync(content.WriterId);

            AiProvenanceMark? existing = await _provenanceService.GetMarkAsync(change.Property, AiProvenanceState.Draft);

            // An AI generated mark never becomes AI modified automatically.
            AiOrigin origin = existing?.Origin == AiOrigin.AiGenerated ? AiOrigin.AiGenerated : change.Origin;

            await _provenanceService.SetMarkAsync(change.Property, origin, AiProvenanceSource.UmbracoAI, userKey, change.Provider, change.Model);
            _logger.LogDebug("Marked {PropertyAlias} on {DocumentKey} as {Origin} after an Umbraco.AI agent changed it.",
                change.Property.PropertyAlias, change.Property.DocumentKey, origin);

            if (change.Proposal is not null)
            {
                _proposals.Remove(change.Proposal);
            }
        }
    }

    /// <summary>
    /// Finds the changed values in an editor's save that match a value the Copilot proposed to the same editor.
    /// A value the editor rewrote before saving does not match, so it isn't marked.
    /// </summary>
    private List<PendingChange> FindProposedChanges(ContentSavingNotification notification)
    {
        var changes = new List<PendingChange>();
        Guid? userKey = _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser?.Key;
        if (userKey is null)
        {
            return changes;
        }

        foreach (IContent content in notification.SavedEntities)
        {
            IReadOnlyList<AiValueProposal> proposals = _proposals.Get(userKey.Value, content.Key);
            if (proposals.Count == 0)
            {
                continue;
            }

            IContent? existing = content.HasIdentity ? _contentService.GetById(content.Key) : null;
            changes.AddRange(MatchProposals(content, existing, proposals));
        }

        return changes;
    }

    /// <summary>
    /// Compares each proposal with the saved value of its property. A match whose value changed in this save is an
    /// AI change: AI generated if the value was empty before, otherwise AI modified.
    /// </summary>
    internal static IEnumerable<PendingChange> MatchProposals(IContent content, IContent? existing, IEnumerable<AiValueProposal> proposals)
    {
        foreach (AiValueProposal proposal in proposals)
        {
            IProperty? property = content.Properties.FirstOrDefault(x =>
                string.Equals(x.Alias, proposal.RootPropertyAlias, StringComparison.OrdinalIgnoreCase));
            if (property is null)
            {
                continue;
            }

            // Only a property that varies by culture or segment has one in its value and its mark.
            string? culture = property.PropertyType.VariesByCulture() ? proposal.Culture : null;
            string? segment = property.PropertyType.VariesBySegment() ? proposal.Segment : null;

            object? newValue = content.GetValue(property.Alias, culture, segment);
            object? oldValue = existing?.GetValue(property.Alias, culture, segment);

            string? newText = proposal.ElementKey is { } key ? ValueText.FindBlockValue(newValue, key, proposal.PropertyAlias) : ValueText.Normalize(newValue);
            string? oldText = proposal.ElementKey is { } oldKey ? ValueText.FindBlockValue(oldValue, oldKey, proposal.PropertyAlias) : ValueText.Normalize(oldValue);

            if (newText is null || string.Equals(newText, proposal.Value, StringComparison.Ordinal) is false
                || string.Equals(newText, oldText, StringComparison.Ordinal))
            {
                continue;
            }

            yield return new PendingChange(
                new AiPropertyReference(content.Key, proposal.PropertyAlias, culture, segment, proposal.ElementKey),
                string.IsNullOrEmpty(oldText) ? AiOrigin.AiGenerated : AiOrigin.AiModified)
            {
                Provider = proposal.Provider,
                Model = proposal.Model,
                Proposal = proposal,
            };
        }
    }

    /// <summary>
    /// Compares each property value with the saved version. An empty value that gets content is AI generated.
    /// A value with content that changes is AI modified.
    /// </summary>
    internal static IEnumerable<PendingChange> FindChanges(IContent content, IContent? existing)
    {
        foreach (IProperty property in content.Properties)
        {
            IProperty? before = existing?.Properties.FirstOrDefault(x => x.Alias == property.Alias);

            foreach (IPropertyValue value in property.Values)
            {
                object? newValue = value.EditedValue;
                object? oldValue = before?.GetValue(value.Culture, value.Segment);

                if (IsEmpty(newValue) || AreEqual(oldValue, newValue))
                {
                    continue;
                }

                yield return new PendingChange(
                    new AiPropertyReference(content.Key, property.Alias, value.Culture, value.Segment),
                    IsEmpty(oldValue) ? AiOrigin.AiGenerated : AiOrigin.AiModified);
            }
        }
    }

    private async Task<Guid> GetUserKeyAsync(int userId)
    {
        try
        {
            return await _userIdKeyResolver.GetAsync(userId);
        }
        catch (InvalidOperationException)
        {
            return Guid.Empty;
        }
    }

    private static bool IsEmpty(object? value) => value switch
    {
        null => true,
        string text => string.IsNullOrWhiteSpace(text),
        _ => false,
    };

    private static bool AreEqual(object? a, object? b) =>
        Equals(a, b) || string.Equals(Serialize(a), Serialize(b), StringComparison.Ordinal);

    private static string? Serialize(object? value) => value switch
    {
        null => null,
        string text => text,
        _ => JsonSerializer.Serialize(value),
    };

    /// <summary>A property value that an agent changed, waiting for the save to succeed.</summary>
    internal sealed record PendingChange(AiPropertyReference Property, AiOrigin Origin)
    {
        public string? Provider { get; init; }

        public string? Model { get; init; }

        /// <summary>The Copilot proposal the change matched, if any. The store forgets it once the save succeeds.</summary>
        public AiValueProposal? Proposal { get; init; }
    }
}

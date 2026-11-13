using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Community.AIProvenance.Models;
using Umbraco.Community.AIProvenance.Services;
using UmbracoAIConstants = Umbraco.AI.Core.Constants;

namespace Umbraco.Community.AIProvenance.UmbracoAI;

/// <summary>
/// Marks the properties that an Umbraco.AI agent changes.
/// </summary>
/// <remarks>
/// Umbraco.AI's content tools save through Umbraco's content services in the same async flow as the agent run,
/// so the AI runtime context is active while the content saves. A save without that context is not an AI write.
/// The handler works out the changes before the save, and sets the marks after the save succeeds.
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
    private readonly IOptionsMonitor<AiProvenanceUmbracoAISettings> _settings;
    private readonly ILogger<AgentContentWriteNotificationHandler> _logger;

    public AgentContentWriteNotificationHandler(
        IAIRuntimeContextAccessor runtimeContextAccessor,
        IContentService contentService,
        IAiProvenanceService provenanceService,
        IUserIdKeyResolver userIdKeyResolver,
        IOptionsMonitor<AiProvenanceUmbracoAISettings> settings,
        ILogger<AgentContentWriteNotificationHandler> logger)
    {
        _runtimeContextAccessor = runtimeContextAccessor;
        _contentService = contentService;
        _provenanceService = provenanceService;
        _userIdKeyResolver = userIdKeyResolver;
        _settings = settings;
        _logger = logger;
    }

    public Task HandleAsync(ContentSavingNotification notification, CancellationToken cancellationToken)
    {
        AIRuntimeContext? runtimeContext = _runtimeContextAccessor.Context;
        if (runtimeContext is null || _settings.CurrentValue.AutomaticMarking is false)
        {
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
    }
}

using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.EntityAdapter;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.Cms.Core.Security;
using UmbracoAIConstants = Umbraco.AI.Core.Constants;

namespace Umbraco.Community.AIProvenance.UmbracoAI.Proposals;

/// <summary>
/// Remembers the values that Umbraco.AI's Copilot proposes with its <c>set_value</c> browser tool.
/// </summary>
/// <remarks>
/// The Copilot's model calls <c>set_value</c> on the server, and the browser then applies the value in the open
/// editor. The save comes later, as an ordinary save with no AI context. This middleware sees the tool call as it
/// passes through Umbraco.AI's chat pipeline, and stores the proposal so the save can be matched to it.
/// The tool's name and arguments belong to Umbraco.AI's Copilot, so a later Copilot version could change them.
/// </remarks>
internal sealed class SetValueProposalChatMiddleware : IAIChatMiddleware
{
    /// <summary>The Copilot browser tool that sets a property value.</summary>
    internal const string SetValueToolName = "set_value";

    private readonly IAIRuntimeContextAccessor _runtimeContextAccessor;
    private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;
    private readonly AiValueProposalStore _store;
    private readonly IOptionsMonitor<AiProvenanceUmbracoAISettings> _settings;
    private readonly ILogger<SetValueProposalChatMiddleware> _logger;

    public SetValueProposalChatMiddleware(
        IAIRuntimeContextAccessor runtimeContextAccessor,
        IBackOfficeSecurityAccessor backOfficeSecurityAccessor,
        AiValueProposalStore store,
        IOptionsMonitor<AiProvenanceUmbracoAISettings> settings,
        ILogger<SetValueProposalChatMiddleware> logger)
    {
        _runtimeContextAccessor = runtimeContextAccessor;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
        _store = store;
        _settings = settings;
        _logger = logger;
    }

    public IChatClient Apply(IChatClient client) => new ProposalCapturingChatClient(client, this);

    /// <summary>Records each <c>set_value</c> call in a model response.</summary>
    internal void Capture(IEnumerable<AIContent> contents)
    {
        if (_settings.CurrentValue.AutomaticMarking is false)
        {
            return;
        }

        foreach (FunctionCallContent call in contents.OfType<FunctionCallContent>())
        {
            if (string.Equals(call.Name, SetValueToolName, StringComparison.Ordinal) is false)
            {
                continue;
            }

            try
            {
                AiValueProposal? proposal = CreateProposal(call, _runtimeContextAccessor.Context, _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser?.Key);
                if (proposal is not null)
                {
                    _store.Add(proposal);
                    _logger.LogDebug("Remembered an AI proposal for {PropertyAlias} on {DocumentKey}.", proposal.PropertyAlias, proposal.DocumentKey);
                }
            }
            catch (Exception exception) when (exception is JsonException or InvalidOperationException or FormatException)
            {
                // A tool call the add-on doesn't understand must never break the Copilot.
                _logger.LogDebug(exception, "Could not read a {ToolName} tool call.", SetValueToolName);
            }
        }
    }

    /// <summary>
    /// Builds a proposal from a <c>set_value</c> call. Returns null when the call isn't for a document's property,
    /// or proposes an empty value.
    /// </summary>
    internal static AiValueProposal? CreateProposal(FunctionCallContent call, AIRuntimeContext? context, Guid? userKey)
    {
        if (context is null || userKey is null || call.Arguments is null)
        {
            return null;
        }

        string? entityType = context.GetValue<string>(UmbracoAIConstants.ContextKeys.EntityType);
        if (string.Equals(entityType, "document", StringComparison.OrdinalIgnoreCase) is false
            || context.TryGetValue(UmbracoAIConstants.ContextKeys.EntityId, out Guid documentKey) is false)
        {
            return null;
        }

        if (TryReadPath(Argument(call, "path"), out string? rootAlias, out string? propertyAlias, out Guid? elementKey) is false)
        {
            return null;
        }

        string value = ValueText.Normalize(Argument(call, "value"));
        if (value.Length == 0)
        {
            return null;
        }

        // Without a culture or segment, the Copilot edits the variant the editor is viewing.
        AISerializedEntity? entity = context.GetValue<AISerializedEntity>(UmbracoAIConstants.ContextKeys.SerializedEntity);
        string? culture = ReadString(Argument(call, "culture")) ?? entity?.Culture;
        string? segment = ReadString(Argument(call, "segment")) ?? entity?.Segment;

        return new AiValueProposal(
            userKey.Value,
            documentKey,
            propertyAlias!,
            rootAlias!,
            elementKey,
            string.IsNullOrWhiteSpace(culture) ? null : culture,
            string.IsNullOrWhiteSpace(segment) ? null : segment,
            value,
            context.GetValue<object>(UmbracoAIConstants.ContextKeys.ProviderId)?.ToString(),
            context.GetValue<object>(UmbracoAIConstants.ContextKeys.ModelId)?.ToString());
    }

    /// <summary>
    /// Reads the tool's path: property aliases at even positions, and <c>{ blockKey }</c> objects at odd positions.
    /// </summary>
    private static bool TryReadPath(object? path, out string? rootAlias, out string? propertyAlias, out Guid? elementKey)
    {
        rootAlias = propertyAlias = null;
        elementKey = null;

        List<JsonElement> entries = path switch
        {
            JsonElement { ValueKind: JsonValueKind.Array } array => array.EnumerateArray().ToList(),
            JsonElement { ValueKind: JsonValueKind.String } single => [single],
            string text => [JsonSerializer.SerializeToElement(text)],
            _ => [],
        };

        // The path ends on a property alias, so it has an odd number of entries.
        if (entries.Count == 0 || entries.Count % 2 == 0)
        {
            return false;
        }

        rootAlias = ReadString(entries[0]);
        propertyAlias = ReadString(entries[^1]);
        if (string.IsNullOrWhiteSpace(rootAlias) || string.IsNullOrWhiteSpace(propertyAlias))
        {
            return false;
        }

        if (entries.Count > 1)
        {
            JsonElement selector = entries[^2];
            if (selector.ValueKind != JsonValueKind.Object
                || selector.TryGetProperty("blockKey", out JsonElement blockKey) is false
                || Guid.TryParse(blockKey.GetString(), out Guid parsed) is false)
            {
                return false;
            }

            elementKey = parsed;
        }

        return true;
    }

    private static object? Argument(FunctionCallContent call, string name) =>
        call.Arguments is not null && call.Arguments.TryGetValue(name, out object? value) ? value : null;

    private static string? ReadString(object? value) => value switch
    {
        string text => text,
        JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
        _ => null,
    };

    /// <summary>Passes every request on unchanged, and looks at the responses for <c>set_value</c> calls.</summary>
    private sealed class ProposalCapturingChatClient : DelegatingChatClient
    {
        private readonly SetValueProposalChatMiddleware _middleware;

        public ProposalCapturingChatClient(IChatClient innerClient, SetValueProposalChatMiddleware middleware)
            : base(innerClient) => _middleware = middleware;

        public override async Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            ChatResponse response = await base.GetResponseAsync(messages, options, cancellationToken);
            _middleware.Capture(response.Messages.SelectMany(message => message.Contents));
            return response;
        }

        public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (ChatResponseUpdate update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
            {
                _middleware.Capture(update.Contents);
                yield return update;
            }
        }
    }
}

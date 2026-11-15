using Umbraco.Cms.Core.Cache;
using Umbraco.Extensions;

namespace Umbraco.Community.AIProvenance.UmbracoAI.Proposals;

/// <summary>
/// Remembers values that an AI proposed in the browser, until the editor saves the document.
/// </summary>
/// <remarks>
/// Umbraco.AI's Copilot proposes a value in one request and the editor saves it in a later one. The proposals live
/// in the server's memory. Umbraco's load-balancing setup runs the backoffice on one server, so both requests reach
/// the same store. A proposal expires after <see cref="Lifetime"/>, and a restart forgets it.
/// </remarks>
internal sealed class AiValueProposalStore
{
    /// <summary>How long a proposal waits for a save.</summary>
    internal static readonly TimeSpan Lifetime = TimeSpan.FromHours(1);

    private readonly IAppPolicyCache _cache;
    private readonly object _lock = new();

    public AiValueProposalStore(AppCaches appCaches) => _cache = appCaches.IsolatedCaches.GetOrCreate<AiValueProposalStore>();

    /// <summary>Remembers a proposal. A newer proposal for the same property value replaces an older one.</summary>
    public void Add(AiValueProposal proposal)
    {
        lock (_lock)
        {
            List<AiValueProposal> proposals = Get(proposal.UserKey, proposal.DocumentKey)
                .Where(x => SameTarget(x, proposal) is false)
                .Append(proposal)
                .ToList();
            _cache.Insert(CacheKey(proposal.UserKey, proposal.DocumentKey), () => proposals, Lifetime);
        }
    }

    /// <summary>Gets the user's open proposals for a document.</summary>
    public IReadOnlyList<AiValueProposal> Get(Guid userKey, Guid documentKey) =>
        _cache.GetCacheItem<List<AiValueProposal>>(CacheKey(userKey, documentKey)) ?? [];

    /// <summary>Forgets a proposal once a save has used it.</summary>
    public void Remove(AiValueProposal proposal)
    {
        lock (_lock)
        {
            List<AiValueProposal> proposals = Get(proposal.UserKey, proposal.DocumentKey).Where(x => x != proposal).ToList();
            string key = CacheKey(proposal.UserKey, proposal.DocumentKey);
            if (proposals.Count == 0)
            {
                _cache.Clear(key);
            }
            else
            {
                _cache.Insert(key, () => proposals, Lifetime);
            }
        }
    }

    private static bool SameTarget(AiValueProposal a, AiValueProposal b) =>
        a.ElementKey == b.ElementKey
        && string.Equals(a.PropertyAlias, b.PropertyAlias, StringComparison.OrdinalIgnoreCase)
        && string.Equals(a.Culture, b.Culture, StringComparison.OrdinalIgnoreCase)
        && string.Equals(a.Segment, b.Segment, StringComparison.OrdinalIgnoreCase);

    private static string CacheKey(Guid userKey, Guid documentKey) => $"AiProvenance.Proposals.{userKey}.{documentKey}";
}

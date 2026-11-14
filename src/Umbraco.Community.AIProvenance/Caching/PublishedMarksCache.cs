using Umbraco.Cms.Core.Cache;
using Umbraco.Community.AIProvenance.Models;
using Umbraco.Extensions;

namespace Umbraco.Community.AIProvenance.Caching;

/// <summary>
/// Keeps each document's published marks in memory across requests, so a page that lists many documents
/// does not read the database once per document.
/// </summary>
/// <remarks>
/// Published marks only change when a document is published, unpublished, copied or deleted. The package clears a
/// document's entry after those operations, and again when Umbraco refreshes its content cache, which happens on
/// every server. Entries also expire after <see cref="Lifetime"/>, so a missed clear corrects itself.
/// </remarks>
internal sealed class PublishedMarksCache
{
    /// <summary>How long an entry lives before it is read from the database again.</summary>
    internal static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    private readonly IAppPolicyCache _cache;

    public PublishedMarksCache(AppCaches appCaches) => _cache = appCaches.IsolatedCaches.GetOrCreate<PublishedMarksCache>();

    /// <summary>Gets a document's published marks, and reads them with <paramref name="read"/> when they are not cached.</summary>
    public IReadOnlyCollection<AiProvenanceMark> Get(Guid documentKey, Func<IReadOnlyCollection<AiProvenanceMark>> read) =>
        _cache.GetCacheItem(CacheKey(documentKey), read, Lifetime) ?? [];

    /// <summary>Removes one document's published marks from the cache.</summary>
    public void Clear(Guid documentKey) => _cache.Clear(CacheKey(documentKey));

    /// <summary>Removes every document's published marks from the cache.</summary>
    public void ClearAll() => _cache.Clear();

    private static string CacheKey(Guid documentKey) => $"AiProvenance.PublishedMarks.{documentKey}";
}

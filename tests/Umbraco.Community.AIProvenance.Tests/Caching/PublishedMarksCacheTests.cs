using NUnit.Framework;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services.Changes;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Community.AIProvenance.Caching;
using Umbraco.Community.AIProvenance.Models;
using Umbraco.Extensions;

namespace Umbraco.Community.AIProvenance.Tests.Caching;

/// <summary>
/// Tests that published marks are cached across reads, and that content operations clear the cache.
/// </summary>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest, WithApplication = true)]
public sealed class PublishedMarksCacheTests : AiProvenanceTestBase
{
    private PublishedMarksCache Cache => GetRequiredService<PublishedMarksCache>();

    private int _databaseReads;

    // Umbraco's integration tests turn caching off, which would make every read reach the database.
    // These tests need a cache that keeps entries.
    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        base.CustomTestSetup(builder);
        builder.Services.AddUnique(new AppCaches(
            new ObjectCacheAppCache(),
            NoAppCache.Instance,
            new IsolatedCaches(_ => new ObjectCacheAppCache())));
    }

    [SetUp]
    public void ResetReads() => _databaseReads = 0;

    /// <summary>Reads a document's published marks through the cache, and counts the database reads.</summary>
    private IReadOnlyCollection<AiProvenanceMark> Read(Guid documentKey) =>
        Cache.Get(documentKey, () =>
        {
            _databaseReads++;
            return ProvenanceService.GetMarksAsync(documentKey, AiProvenanceState.Published).GetAwaiter().GetResult();
        });

    private Task Mark(Guid documentKey, string alias) =>
        ProvenanceService.SetMarkAsync(new AiPropertyReference(documentKey, alias), AiOrigin.AiGenerated, AiProvenanceSource.Manual, UserKey);

    [Test]
    public async Task Reads_the_database_once_for_repeated_reads()
    {
        await Mark(Textpage.Key, "title");
        ContentService.Publish(Textpage, ["*"]);

        Read(Textpage.Key);
        IReadOnlyCollection<AiProvenanceMark> marks = Read(Textpage.Key);

        Assert.That(marks, Has.Count.EqualTo(1));
        Assert.That(_databaseReads, Is.EqualTo(1));
    }

    [Test]
    public async Task Publishing_clears_the_cached_marks()
    {
        Assert.That(Read(Textpage.Key), Is.Empty, "The cache holds the page's empty marks.");

        await Mark(Textpage.Key, "title");
        ContentService.Publish(Textpage, ["*"]);

        Assert.That(Read(Textpage.Key), Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Unpublishing_clears_the_cached_marks()
    {
        await Mark(Textpage.Key, "title");
        ContentService.Publish(Textpage, ["*"]);
        Assert.That(Read(Textpage.Key), Has.Count.EqualTo(1));

        ContentService.Unpublish(Textpage);

        Assert.That(Read(Textpage.Key), Is.Empty);
    }

    [Test]
    public async Task Deleting_clears_the_cached_marks()
    {
        await Mark(Textpage.Key, "title");
        ContentService.Publish(Textpage, ["*"]);
        Assert.That(Read(Textpage.Key), Has.Count.EqualTo(1));

        ContentService.Delete(Textpage);

        Assert.That(Read(Textpage.Key), Is.Empty);
    }

    [Test]
    public void A_content_cache_refresh_clears_only_the_changed_document()
    {
        Read(Textpage.Key);
        Read(Subpage.Key);

        Refresh(new ContentCacheRefresher.JsonPayload { Id = Textpage.Id, Key = Textpage.Key, ChangeTypes = TreeChangeTypes.RefreshNode });
        _databaseReads = 0;
        Read(Textpage.Key);
        Read(Subpage.Key);

        Assert.That(_databaseReads, Is.EqualTo(1), "Only the refreshed page is read again.");
    }

    [TestCase(TreeChangeTypes.RefreshAll)]
    [TestCase(TreeChangeTypes.RefreshBranch)]
    public void A_branch_or_full_refresh_clears_every_document(TreeChangeTypes changeTypes)
    {
        Read(Textpage.Key);
        Read(Subpage.Key);

        Refresh(new ContentCacheRefresher.JsonPayload { Id = Textpage.Id, Key = Textpage.Key, ChangeTypes = changeTypes });
        _databaseReads = 0;
        Read(Textpage.Key);
        Read(Subpage.Key);

        Assert.That(_databaseReads, Is.EqualTo(2));
    }

    private void Refresh(params ContentCacheRefresher.JsonPayload[] payloads) =>
        new PublishedMarksCacheRefresherHandler(Cache).Handle(new ContentCacheRefresherNotification(payloads, MessageType.RefreshByPayload));
}

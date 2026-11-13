using NUnit.Framework;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Community.AIProvenance.Models;

namespace Umbraco.Community.AIProvenance.Tests;

/// <summary>
/// Tests the service directly, without Umbraco content operations.
/// </summary>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public sealed class AiProvenanceServiceTests : AiProvenanceTestBase
{
    private static readonly Guid DocumentKey = Guid.Parse("8f0c4d8e-5d43-4c0b-9a59-2a8f7f1b6a10");

    private static AiPropertyReference Property(string alias, string? culture = null, string? segment = null, Guid? elementKey = null) =>
        new(DocumentKey, alias, culture, segment, elementKey);

    private Task Mark(AiPropertyReference property, AiOrigin origin = AiOrigin.AiGenerated) =>
        ProvenanceService.SetMarkAsync(property, origin, AiProvenanceSource.Manual, UserKey);

    [Test]
    public async Task Set_then_get_returns_the_draft_mark()
    {
        await ProvenanceService.SetMarkAsync(
            Property("summary"), AiOrigin.AiGenerated, AiProvenanceSource.UmbracoAI, UserKey, "openai", "gpt-test");

        AiProvenanceMark? mark = await ProvenanceService.GetMarkAsync(Property("summary"), AiProvenanceState.Draft);

        Assert.That(mark, Is.Not.Null);
        Assert.That(mark!.Origin, Is.EqualTo(AiOrigin.AiGenerated));
        Assert.That(mark.Source, Is.EqualTo(AiProvenanceSource.UmbracoAI));
        Assert.That(mark.AiProvider, Is.EqualTo("openai"));
        Assert.That(mark.AiModel, Is.EqualTo("gpt-test"));
        Assert.That(mark.UpdatedBy, Is.EqualTo(UserKey));
        Assert.That(await ProvenanceService.GetMarkAsync(Property("summary"), AiProvenanceState.Published), Is.Null);
    }

    [Test]
    public async Task Set_replaces_an_existing_mark()
    {
        await Mark(Property("summary"), AiOrigin.AiGenerated);
        await Mark(Property("summary"), AiOrigin.AiModified);

        IReadOnlyCollection<AiProvenanceMark> marks = await ProvenanceService.GetMarksAsync(DocumentKey, AiProvenanceState.Draft);

        Assert.That(marks, Has.Count.EqualTo(1));
        Assert.That(marks.Single().Origin, Is.EqualTo(AiOrigin.AiModified));
    }

    [Test]
    public async Task Setting_none_or_removing_deletes_the_mark()
    {
        await Mark(Property("summary"));
        await Mark(Property("title"));

        await ProvenanceService.SetMarkAsync(Property("summary"), AiOrigin.None, AiProvenanceSource.Manual, UserKey);
        await ProvenanceService.RemoveMarkAsync(Property("title"));

        Assert.That(await ProvenanceService.GetMarksAsync(DocumentKey, AiProvenanceState.Draft), Is.Empty);
    }

    [Test]
    public async Task Cultures_segments_and_blocks_are_separate_marks()
    {
        var elementKey = Guid.NewGuid();
        await Mark(Property("summary"));
        await Mark(Property("summary", "en-US"));
        await Mark(Property("summary", "da-DK"), AiOrigin.AiModified);
        await Mark(Property("summary", "en-US", "mobile"));
        await Mark(Property("summary", elementKey: elementKey));

        Assert.That(await ProvenanceService.GetMarksAsync(DocumentKey, AiProvenanceState.Draft), Has.Count.EqualTo(5));
        Assert.That(
            (await ProvenanceService.GetMarkAsync(Property("summary", "da-DK"), AiProvenanceState.Draft))!.Origin,
            Is.EqualTo(AiOrigin.AiModified));
        Assert.That(await ProvenanceService.GetMarkAsync(Property("summary", "en-US", "desktop"), AiProvenanceState.Draft), Is.Null);
    }

    [Test]
    public async Task Culture_lookups_ignore_case()
    {
        await Mark(Property("summary", "en-US"));

        Assert.That(await ProvenanceService.GetMarkAsync(Property("summary", "EN-us"), AiProvenanceState.Draft), Is.Not.Null);
    }

    [Test]
    public async Task Publish_all_copies_every_draft_mark()
    {
        await Mark(Property("summary"));
        await Mark(Property("summary", "en-US"));
        await Mark(Property("summary", "da-DK"));

        await ProvenanceService.PublishAsync(DocumentKey, cultures: null);

        Assert.That(await ProvenanceService.GetMarksAsync(DocumentKey, AiProvenanceState.Published), Has.Count.EqualTo(3));
    }

    [Test]
    public async Task Publish_replaces_published_marks_that_were_removed_from_the_draft()
    {
        await Mark(Property("summary"));
        await ProvenanceService.PublishAsync(DocumentKey, cultures: null);

        await ProvenanceService.RemoveMarkAsync(Property("summary"));
        await ProvenanceService.PublishAsync(DocumentKey, cultures: null);

        Assert.That(await ProvenanceService.GetMarksAsync(DocumentKey, AiProvenanceState.Published), Is.Empty);
    }

    [Test]
    public async Task Publish_one_culture_copies_that_culture_and_invariant_marks_only()
    {
        await Mark(Property("title"));
        await Mark(Property("summary", "en-US"));
        await Mark(Property("summary", "da-DK"));

        await ProvenanceService.PublishAsync(DocumentKey, ["en-US"]);

        IReadOnlyCollection<AiProvenanceMark> published = await ProvenanceService.GetMarksAsync(DocumentKey, AiProvenanceState.Published);
        Assert.That(published.Select(x => x.Property.Culture), Is.EquivalentTo(new string?[] { null, "en-us" }));
    }

    [Test]
    public async Task Publish_one_culture_leaves_other_published_cultures_alone()
    {
        await Mark(Property("summary", "da-DK"));
        await ProvenanceService.PublishAsync(DocumentKey, ["da-DK"]);

        // An editor removes the Danish mark in the draft, then publishes English only.
        await ProvenanceService.RemoveMarkAsync(Property("summary", "da-DK"));
        await ProvenanceService.PublishAsync(DocumentKey, ["en-US"]);

        Assert.That(await ProvenanceService.GetMarkAsync(Property("summary", "da-DK"), AiProvenanceState.Published), Is.Not.Null);
    }

    [Test]
    public async Task Unpublish_one_culture_keeps_invariant_and_other_cultures()
    {
        await Mark(Property("title"));
        await Mark(Property("summary", "en-US"));
        await Mark(Property("summary", "da-DK"));
        await ProvenanceService.PublishAsync(DocumentKey, cultures: null);

        await ProvenanceService.UnpublishAsync(DocumentKey, ["da-DK"]);

        IReadOnlyCollection<AiProvenanceMark> published = await ProvenanceService.GetMarksAsync(DocumentKey, AiProvenanceState.Published);
        Assert.That(published.Select(x => x.Property.Culture), Is.EquivalentTo(new string?[] { null, "en-us" }));
    }

    [Test]
    public async Task Unpublish_whole_document_removes_published_marks_and_keeps_drafts()
    {
        await Mark(Property("title"));
        await Mark(Property("summary", "en-US"));
        await ProvenanceService.PublishAsync(DocumentKey, cultures: null);

        await ProvenanceService.UnpublishAsync(DocumentKey, cultures: null);

        Assert.That(await ProvenanceService.GetMarksAsync(DocumentKey, AiProvenanceState.Published), Is.Empty);
        Assert.That(await ProvenanceService.GetMarksAsync(DocumentKey, AiProvenanceState.Draft), Has.Count.EqualTo(2));
    }

    [Test]
    public async Task Copy_copies_draft_marks_except_block_marks()
    {
        var targetKey = Guid.NewGuid();
        await Mark(Property("title"));
        await Mark(Property("summary", "en-US"));
        await Mark(Property("text", elementKey: Guid.NewGuid()));

        await ProvenanceService.CopyAsync(DocumentKey, targetKey);

        IReadOnlyCollection<AiProvenanceMark> copied = await ProvenanceService.GetMarksAsync(targetKey, AiProvenanceState.Draft);
        Assert.That(copied.Select(x => x.Property.PropertyAlias), Is.EquivalentTo(new[] { "title", "summary" }));
        Assert.That(copied.All(x => x.Property.DocumentKey == targetKey), Is.True);
    }

    [Test]
    public async Task Delete_all_removes_draft_and_published_marks()
    {
        await Mark(Property("title"));
        await ProvenanceService.PublishAsync(DocumentKey, cultures: null);

        await ProvenanceService.DeleteAllAsync(DocumentKey);

        Assert.That(await ProvenanceService.GetMarksAsync(DocumentKey, AiProvenanceState.Draft), Is.Empty);
        Assert.That(await ProvenanceService.GetMarksAsync(DocumentKey, AiProvenanceState.Published), Is.Empty);
    }
}

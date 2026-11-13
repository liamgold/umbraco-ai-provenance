using NUnit.Framework;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Community.AIProvenance.Models;

namespace Umbraco.Community.AIProvenance.Tests;

/// <summary>
/// Tests that the marks follow real Umbraco content operations.
/// </summary>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest, WithApplication = true)]
public sealed class ContentNotificationTests : AiProvenanceTestBase
{
    private ILanguageService LanguageService => GetRequiredService<ILanguageService>();

    private Task Mark(Guid documentKey, string alias, string? culture = null) =>
        ProvenanceService.SetMarkAsync(
            new AiPropertyReference(documentKey, alias, culture), AiOrigin.AiGenerated, AiProvenanceSource.Manual, UserKey);

    private Task<IReadOnlyCollection<AiProvenanceMark>> Published(Guid documentKey) =>
        ProvenanceService.GetMarksAsync(documentKey, AiProvenanceState.Published);

    [Test]
    public async Task Publishing_copies_draft_marks_to_published()
    {
        await Mark(Textpage.Key, "title");

        ContentService.Publish(Textpage, ["*"]);

        Assert.That(await Published(Textpage.Key), Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Unpublishing_removes_published_marks()
    {
        await Mark(Textpage.Key, "title");
        ContentService.Publish(Textpage, ["*"]);

        ContentService.Unpublish(Textpage);

        Assert.That(await Published(Textpage.Key), Is.Empty);
        Assert.That(await ProvenanceService.GetMarksAsync(Textpage.Key, AiProvenanceState.Draft), Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Deleting_removes_all_marks()
    {
        await Mark(Textpage.Key, "title");
        ContentService.Publish(Textpage, ["*"]);

        ContentService.Delete(Textpage);

        Assert.That(await ProvenanceService.GetMarksAsync(Textpage.Key, AiProvenanceState.Draft), Is.Empty);
        Assert.That(await Published(Textpage.Key), Is.Empty);
    }

    [Test]
    public async Task Copying_copies_draft_marks_to_the_copy()
    {
        await Mark(Textpage.Key, "title");

        IContent? copy = ContentService.Copy(Textpage, -1, relateToOriginal: false);

        Assert.That(copy, Is.Not.Null);
        Assert.That(await ProvenanceService.GetMarksAsync(copy!.Key, AiProvenanceState.Draft), Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Publishing_and_unpublishing_cultures_follow_the_culture()
    {
        await LanguageService.CreateAsync(new LanguageBuilder().WithCultureInfo("da-DK").Build(), UserKey);

        ContentType variantType = ContentTypeBuilder.CreateBasicContentType("variantPage", "Variant Page");
        variantType.Variations = ContentVariation.Culture;
        ContentTypeService.Save(variantType);

        Content page = ContentBuilder.CreateBasicContent(variantType);
        page.SetCultureName("Page", "en-US");
        page.SetCultureName("Side", "da-DK");
        ContentService.Save(page);

        await Mark(page.Key, "title");
        await Mark(page.Key, "summary", "en-US");
        await Mark(page.Key, "summary", "da-DK");

        ContentService.Publish(page, ["en-US"]);

        Assert.That(
            (await Published(page.Key)).Select(x => x.Property.Culture),
            Is.EquivalentTo(new string?[] { null, "en-us" }),
            "Publishing English copies the English and invariant marks.");

        ContentService.Publish(page, ["da-DK"]);
        Assert.That(await Published(page.Key), Has.Count.EqualTo(3), "Publishing Danish adds the Danish mark.");

        ContentService.Unpublish(page, "da-DK");

        Assert.That(
            (await Published(page.Key)).Select(x => x.Property.Culture),
            Is.EquivalentTo(new string?[] { null, "en-us" }),
            "Unpublishing Danish removes only the Danish mark.");
    }
}

using Moq;
using NUnit.Framework;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Community.AIProvenance.Models;
using Umbraco.Community.AIProvenance.Rendering;
using Umbraco.Community.AIProvenance.Services;

namespace Umbraco.Community.AIProvenance.Tests.Rendering;

[TestFixture]
public sealed class AiProvenanceReaderTests
{
    private static readonly Guid DocumentKey = Guid.Parse("0c7a2a35-4d9f-4b8e-9c0e-0f1a2b3c4d5e");

    private readonly List<AiProvenanceMark> _published = [];
    private readonly List<AiProvenanceMark> _draft = [];
    private VariationContext _variationContext = new("en-US");
    private bool _inPreview;

    [SetUp]
    public void Reset()
    {
        _published.Clear();
        _draft.Clear();
        _variationContext = new VariationContext("en-US");
        _inPreview = false;
    }

    private AiProvenanceReader CreateReader()
    {
        var service = new Mock<IAiProvenanceService>();
        service.Setup(x => x.GetMarksAsync(It.IsAny<Guid>(), AiProvenanceState.Published)).ReturnsAsync(() => _published.ToArray());
        service.Setup(x => x.GetMarksAsync(It.IsAny<Guid>(), AiProvenanceState.Draft)).ReturnsAsync(() => _draft.ToArray());

        var variationContextAccessor = new Mock<IVariationContextAccessor>();
        variationContextAccessor.SetupGet(x => x.VariationContext).Returns(() => _variationContext);

        var umbracoContext = new Mock<IUmbracoContext>();
        umbracoContext.SetupGet(x => x.InPreviewMode).Returns(() => _inPreview);
        IUmbracoContext? context = umbracoContext.Object;
        var umbracoContextAccessor = new Mock<IUmbracoContextAccessor>();
        umbracoContextAccessor.Setup(x => x.TryGetUmbracoContext(out context)).Returns(true);

        return new AiProvenanceReader(service.Object, variationContextAccessor.Object, umbracoContextAccessor.Object, AppCaches.NoCache);
    }

    private static Mock<IPublishedContent> Content(Guid key, params (string Alias, ContentVariation Variation)[] properties)
    {
        var content = new Mock<IPublishedContent>();
        content.SetupGet(x => x.Key).Returns(key);
        foreach ((string alias, ContentVariation variation) in properties)
        {
            var propertyType = new Mock<IPublishedPropertyType>();
            propertyType.SetupGet(x => x.Variations).Returns(variation);
            var property = new Mock<IPublishedProperty>();
            property.SetupGet(x => x.Alias).Returns(alias);
            property.SetupGet(x => x.PropertyType).Returns(propertyType.Object);
            content.Setup(x => x.GetProperty(alias)).Returns(property.Object);
        }

        return content;
    }

    private static AiProvenanceMark Mark(string alias, AiOrigin origin, string? culture = null, Guid? elementKey = null) => new()
    {
        Property = new AiPropertyReference(DocumentKey, alias, culture, null, elementKey),
        State = AiProvenanceState.Published,
        Origin = origin,
        Source = AiProvenanceSource.Manual,
        UpdatedBy = Guid.Empty,
        UpdatedUtc = DateTime.UtcNow,
    };

    [Test]
    public void Invariant_property_uses_the_invariant_mark()
    {
        _published.Add(Mark("summary", AiOrigin.AiGenerated));
        IPublishedContent content = Content(DocumentKey, ("summary", ContentVariation.Nothing)).Object;

        Assert.That(CreateReader().GetOrigin(content, "summary"), Is.EqualTo(AiOrigin.AiGenerated));
    }

    [Test]
    public void Variant_property_uses_the_current_culture()
    {
        _published.Add(Mark("heading", AiOrigin.AiGenerated, "da-dk"));
        IPublishedContent content = Content(DocumentKey, ("heading", ContentVariation.Culture)).Object;
        AiProvenanceReader reader = CreateReader();

        Assert.That(reader.GetOrigin(content, "heading"), Is.EqualTo(AiOrigin.None), "English has no mark.");

        _variationContext = new VariationContext("da-DK");
        Assert.That(reader.GetOrigin(content, "heading"), Is.EqualTo(AiOrigin.AiGenerated), "Danish has a mark.");
    }

    [Test]
    public void An_explicit_culture_overrides_the_current_culture()
    {
        _published.Add(Mark("heading", AiOrigin.AiModified, "da-dk"));
        IPublishedContent content = Content(DocumentKey, ("heading", ContentVariation.Culture)).Object;

        Assert.That(CreateReader().GetOrigin(content, "heading", culture: "da-DK"), Is.EqualTo(AiOrigin.AiModified));
    }

    [Test]
    public void Unknown_property_has_no_origin()
    {
        _published.Add(Mark("summary", AiOrigin.AiGenerated));
        IPublishedContent content = Content(DocumentKey).Object;

        Assert.That(CreateReader().GetOrigin(content, "summary"), Is.EqualTo(AiOrigin.None));
    }

    [Test]
    public void Preview_reads_the_draft_marks()
    {
        _draft.Add(Mark("summary", AiOrigin.AiModified));
        IPublishedContent content = Content(DocumentKey, ("summary", ContentVariation.Nothing)).Object;
        AiProvenanceReader reader = CreateReader();

        Assert.That(reader.GetOrigin(content, "summary"), Is.EqualTo(AiOrigin.None), "The published state has no mark.");

        _inPreview = true;
        Assert.That(reader.GetOrigin(content, "summary"), Is.EqualTo(AiOrigin.AiModified));
    }

    [Test]
    public void Block_properties_match_on_the_element_key()
    {
        var elementKey = Guid.NewGuid();
        _published.Add(Mark("text", AiOrigin.AiGenerated, elementKey: elementKey));
        IPublishedContent document = Content(DocumentKey, ("text", ContentVariation.Nothing)).Object;
        IPublishedContent element = Content(elementKey, ("text", ContentVariation.Nothing)).Object;
        AiProvenanceReader reader = CreateReader();

        Assert.That(reader.GetOrigin(element, document, "text"), Is.EqualTo(AiOrigin.AiGenerated));
        Assert.That(reader.GetOrigin(document, "text"), Is.EqualTo(AiOrigin.None), "The document's own text property has no mark.");
    }

    [Test]
    public void GetOrigins_lists_marked_document_properties_for_the_culture()
    {
        _published.Add(Mark("summary", AiOrigin.AiGenerated));
        _published.Add(Mark("heading", AiOrigin.AiModified, "en-us"));
        _published.Add(Mark("heading", AiOrigin.AiGenerated, "da-dk"));
        _published.Add(Mark("text", AiOrigin.AiGenerated, elementKey: Guid.NewGuid()));
        IPublishedContent content = Content(
            DocumentKey,
            ("summary", ContentVariation.Nothing),
            ("heading", ContentVariation.Culture),
            ("text", ContentVariation.Nothing)).Object;

        IReadOnlyDictionary<string, AiOrigin> origins = CreateReader().GetOrigins(content);

        Assert.That(origins, Is.EquivalentTo(new Dictionary<string, AiOrigin>
        {
            ["summary"] = AiOrigin.AiGenerated,
            ["heading"] = AiOrigin.AiModified,
        }));
    }
}

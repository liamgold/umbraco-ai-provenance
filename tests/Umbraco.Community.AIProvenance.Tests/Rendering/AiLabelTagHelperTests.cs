using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Moq;
using NUnit.Framework;
using Umbraco.Cms.Core.Dictionary;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;
using Umbraco.Community.AIProvenance.Models;
using Umbraco.Community.AIProvenance.Rendering;

namespace Umbraco.Community.AIProvenance.Tests.Rendering;

[TestFixture]
public sealed class AiLabelTagHelperTests
{
    private AiLabelViewModel? _renderedModel;

    private AiLabelTagHelper CreateTagHelper(AiOrigin origin, string translatedText = "")
    {
        IPublishedContent page = new Mock<IPublishedContent>().Object;

        var reader = new Mock<IAiProvenanceReader>();
        reader.Setup(x => x.GetOrigin(page, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>())).Returns(origin);

        var request = new Mock<IPublishedRequest>();
        request.SetupGet(x => x.PublishedContent).Returns(page);
        var umbracoContext = new Mock<IUmbracoContext>();
        umbracoContext.SetupGet(x => x.PublishedRequest).Returns(request.Object);
        IUmbracoContext? context = umbracoContext.Object;
        var umbracoContextAccessor = new Mock<IUmbracoContextAccessor>();
        umbracoContextAccessor.Setup(x => x.TryGetUmbracoContext(out context)).Returns(true);

        var dictionary = new Mock<ICultureDictionary>();
        dictionary.Setup(x => x[It.IsAny<string>()]).Returns(translatedText);
        var dictionaryFactory = new Mock<ICultureDictionaryFactory>();
        dictionaryFactory.Setup(x => x.CreateDictionary()).Returns(dictionary.Object);

        var htmlHelper = new Mock<IHtmlHelper>();
        htmlHelper.As<IViewContextAware>();
        htmlHelper
            .Setup(x => x.PartialAsync(AiLabelTagHelper.PartialViewPath, It.IsAny<object>(), It.IsAny<ViewDataDictionary?>()))
            .Callback<string, object, ViewDataDictionary?>((_, model, _) => _renderedModel = (AiLabelViewModel)model)
            .ReturnsAsync(new HtmlString("[label]"));

        return new AiLabelTagHelper(reader.Object, umbracoContextAccessor.Object, dictionaryFactory.Object, htmlHelper.Object)
        {
            For = "summary",
            ViewContext = new ViewContext(),
        };
    }

    private static async Task<string> RenderAsync(AiLabelTagHelper tagHelper, string childContent)
    {
        var context = new TagHelperContext([], new Dictionary<object, object>(), "id");
        var output = new TagHelperOutput(
            "ai-label",
            [],
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent().SetHtmlContent(childContent)));

        await tagHelper.ProcessAsync(context, output);

        using var writer = new StringWriter();
        output.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
        return writer.ToString();
    }

    [SetUp]
    public void Reset() => _renderedModel = null;

    [Test]
    public async Task Unmarked_property_renders_only_the_content()
    {
        string html = await RenderAsync(CreateTagHelper(AiOrigin.None), "<p>Summary</p>");

        Assert.That(html, Is.EqualTo("<p>Summary</p>"));
        Assert.That(_renderedModel, Is.Null);
    }

    [Test]
    public async Task Marked_property_renders_the_label_before_the_content_by_default()
    {
        string html = await RenderAsync(CreateTagHelper(AiOrigin.AiGenerated), "<p>Summary</p>");

        Assert.That(html, Is.EqualTo("[label]<p>Summary</p>"));
        Assert.That(_renderedModel!.Origin, Is.EqualTo(AiOrigin.AiGenerated));
        Assert.That(_renderedModel.Text, Is.EqualTo("AI generated"));
        Assert.That(_renderedModel.Description, Is.EqualTo("AI generated this content."));
    }

    [Test]
    public async Task Position_after_puts_the_label_after_the_content()
    {
        AiLabelTagHelper tagHelper = CreateTagHelper(AiOrigin.AiModified);
        tagHelper.Position = "after";

        string html = await RenderAsync(tagHelper, "<p>Summary</p>");

        Assert.That(html, Is.EqualTo("<p>Summary</p>[label]"));
        Assert.That(_renderedModel!.Text, Is.EqualTo("AI modified"));
        Assert.That(_renderedModel.Description, Is.EqualTo("Our editors wrote this content and edited it with AI."));
    }

    [Test]
    public async Task Dictionary_items_replace_the_default_text()
    {
        await RenderAsync(CreateTagHelper(AiOrigin.AiGenerated, translatedText: "Genereret med AI"), string.Empty);

        Assert.That(_renderedModel!.Text, Is.EqualTo("Genereret med AI"));
    }
}

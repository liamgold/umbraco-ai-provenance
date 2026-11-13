using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Umbraco.Cms.Core.Dictionary;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Community.AIProvenance.Models;

namespace Umbraco.Community.AIProvenance.Rendering;

/// <summary>
/// Renders an AI label for a property when the property is marked, and nothing extra when it is not.
/// </summary>
/// <example>
/// <code>
/// &lt;ai-label for="summary" /&gt;
/// &lt;ai-label for="summary" position="after"&gt;@Model.Value("summary")&lt;/ai-label&gt;
/// &lt;ai-label for="text" item="@block.Content"&gt;...&lt;/ai-label&gt;
/// </code>
/// </example>
[HtmlTargetElement("ai-label", Attributes = ForAttributeName)]
public sealed class AiLabelTagHelper : TagHelper
{
    private const string ForAttributeName = "for";

    /// <summary>The path of the label's partial view. A site overrides the markup with its own view at this path.</summary>
    public const string PartialViewPath = "~/Views/Partials/AIProvenance/Label.cshtml";

    private readonly IAiProvenanceReader _reader;
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;
    private readonly ICultureDictionaryFactory _cultureDictionaryFactory;
    private readonly IHtmlHelper _htmlHelper;

    public AiLabelTagHelper(
        IAiProvenanceReader reader,
        IUmbracoContextAccessor umbracoContextAccessor,
        ICultureDictionaryFactory cultureDictionaryFactory,
        IHtmlHelper htmlHelper)
    {
        _reader = reader;
        _umbracoContextAccessor = umbracoContextAccessor;
        _cultureDictionaryFactory = cultureDictionaryFactory;
        _htmlHelper = htmlHelper;
    }

    /// <summary>The alias of the property.</summary>
    [HtmlAttributeName(ForAttributeName)]
    public string For { get; set; } = string.Empty;

    /// <summary>
    /// The content that has the property: a block's content element, or a document. The current page by default.
    /// </summary>
    [HtmlAttributeName("item")]
    public IPublishedElement? Item { get; set; }

    /// <summary>The culture, or null for the current culture.</summary>
    [HtmlAttributeName("culture")]
    public string? Culture { get; set; }

    /// <summary>Where the label goes relative to the tag's content: "before" (the default) or "after".</summary>
    [HtmlAttributeName("position")]
    public string Position { get; set; } = "before";

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = null;
        TagHelperContent childContent = await output.GetChildContentAsync();

        AiOrigin origin = GetOrigin();
        if (origin == AiOrigin.None)
        {
            output.Content.SetHtmlContent(childContent);
            return;
        }

        (_htmlHelper as IViewContextAware)?.Contextualize(ViewContext);
        IHtmlContent label = await _htmlHelper.PartialAsync(PartialViewPath, CreateModel(origin));

        bool after = string.Equals(Position, "after", StringComparison.OrdinalIgnoreCase);
        output.Content.AppendHtml(after ? childContent : label);
        output.Content.AppendHtml(after ? label : childContent);
    }

    private AiOrigin GetOrigin()
    {
        IPublishedContent? page = _umbracoContextAccessor.TryGetUmbracoContext(out IUmbracoContext? umbracoContext)
            ? umbracoContext.PublishedRequest?.PublishedContent
            : null;

        return Item switch
        {
            IPublishedContent document => _reader.GetOrigin(document, For, Culture),
            IPublishedElement element when page is not null => _reader.GetOrigin(element, page, For, Culture),
            null when page is not null => _reader.GetOrigin(page, For, Culture),
            _ => AiOrigin.None,
        };
    }

    private AiLabelViewModel CreateModel(AiOrigin origin)
    {
        ICultureDictionary dictionary = _cultureDictionaryFactory.CreateDictionary();
        bool generated = origin == AiOrigin.AiGenerated;

        return new AiLabelViewModel
        {
            Origin = origin,
            PropertyAlias = For,
            Text = Translate(dictionary, generated ? "AIProvenance.AiGenerated" : "AIProvenance.AiModified",
                generated ? "AI generated" : "AI modified"),
            Description = Translate(dictionary, generated ? "AIProvenance.AiGeneratedDescription" : "AIProvenance.AiModifiedDescription",
                generated ? "AI generated this content." : "Our editors wrote this content and edited it with AI."),
        };
    }

    /// <summary>Uses the site's dictionary item when it exists, so sites can translate the label.</summary>
    private static string Translate(ICultureDictionary dictionary, string key, string fallback)
    {
        string? value = dictionary[key];
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}

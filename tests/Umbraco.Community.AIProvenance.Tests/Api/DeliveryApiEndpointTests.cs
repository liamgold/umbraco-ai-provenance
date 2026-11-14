using System.Net;
using System.Net.Http.Json;
using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;
using Umbraco.Extensions;
using Umbraco.Community.AIProvenance.DeliveryApi;
using Umbraco.Community.AIProvenance.Models;
using Umbraco.Community.AIProvenance.Services;

namespace Umbraco.Community.AIProvenance.Tests.Api;

/// <summary>
/// Starts the test server with the Delivery API enabled and a published, marked page.
/// </summary>
public abstract class DeliveryApiTestBase : PackageApiTestBase
{
    protected const string ContentRoot = "/umbraco/delivery/api/v2/content";

    private static readonly Guid SuperUserKey = Umbraco.Cms.Core.Constants.Security.SuperUserKey;

    protected IContentService ContentService => GetRequiredService<IContentService>();

    private IContentTypeService ContentTypeService => GetRequiredService<IContentTypeService>();

    private ILanguageService LanguageService => GetRequiredService<ILanguageService>();

    private IAiProvenanceService ProvenanceService => GetRequiredService<IAiProvenanceService>();

    protected IContent Page { get; private set; } = null!;

    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        builder.Services.Configure<DeliveryApiSettings>(settings => settings.Enabled = true);

        // Integration tests leave out the handler that refreshes the cache after a publish, and their messenger
        // ignores refreshes. Without both, published content never reaches the Delivery API.
        builder.AddNotificationHandler<ContentTreeChangeNotification, ContentTreeChangeDistributedCacheNotificationHandler>();
        builder.Services.AddUnique<IServerMessenger, LocalServerMessenger>();
    }

    /// <summary>
    /// Publishes a page in English and Danish. The culture-variant title is marked in English only,
    /// and the invariant body text is marked AI modified.
    /// </summary>
    [SetUp]
    public async Task CreatePublishedPage()
    {
        // The fixture's tests share a database, so each test adds its own content type, and Danish only once.
        if (await LanguageService.GetAsync("da-DK") is null)
        {
            await LanguageService.CreateAsync(new LanguageBuilder().WithCultureInfo("da-DK").Build(), SuperUserKey);
        }

        ContentType contentType = ContentTypeBuilder.CreateSimpleContentType($"page{Guid.NewGuid():N}", "AIP Delivery Page");
        contentType.Variations = ContentVariation.Culture;
        contentType.PropertyTypes.Single(x => x.Alias == "title").Variations = ContentVariation.Culture;
        contentType.AllowedAsRoot = true;

        // The builder adds a template that the test database does not have.
        contentType.AllowedTemplates = [];
        contentType.SetDefaultTemplate(null);
        await ContentTypeService.CreateAsync(contentType, SuperUserKey);

        Content page = ContentBuilder.CreateBasicContent(contentType);
        page.TemplateId = null;
        page.SetCultureName("Page", "en-US");
        page.SetCultureName("Side", "da-DK");
        page.SetValue("title", "Hello", "en-US");
        page.SetValue("title", "Hej", "da-DK");
        page.SetValue("bodyText", "Body text");
        ContentService.Save(page);

        await ProvenanceService.SetMarkAsync(
            new AiPropertyReference(page.Key, "title", "en-US"), AiOrigin.AiGenerated, AiProvenanceSource.Manual, SuperUserKey);
        await ProvenanceService.SetMarkAsync(
            new AiPropertyReference(page.Key, "bodyText"), AiOrigin.AiModified, AiProvenanceSource.Manual, SuperUserKey);

        PublishResult published = ContentService.Publish(page, ["*"]);
        Assert.That(published.Success, Is.True, $"Could not publish the page: {published.Result}");
        Page = page;
    }

    protected Task<HttpResponseMessage> GetItemAsync(Guid id, string? culture = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{ContentRoot}/item/{id}/ai-provenance");
        if (culture is not null)
        {
            request.Headers.Add("Accept-Language", culture);
        }

        return Client.SendAsync(request);
    }

    protected static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

}

/// <summary>
/// Calls the Delivery API endpoints that return a content item's marked properties.
/// </summary>
public sealed class DeliveryApiEndpointTests : DeliveryApiTestBase
{
    [Test]
    public async Task Returns_the_marked_properties_in_the_requested_culture()
    {
        AiProvenanceResponseModel english = await ReadAsync<AiProvenanceResponseModel>(await GetItemAsync(Page.Key, "en-US"));
        AiProvenanceResponseModel danish = await ReadAsync<AiProvenanceResponseModel>(await GetItemAsync(Page.Key, "da-DK"));

        Assert.Multiple(() =>
        {
            Assert.That(english.Id, Is.EqualTo(Page.Key));
            Assert.That(english.Properties, Is.EquivalentTo(new Dictionary<string, string>
            {
                ["title"] = "aiGenerated",
                ["bodyText"] = "aiModified",
            }));
            Assert.That(danish.Properties, Is.EquivalentTo(new Dictionary<string, string> { ["bodyText"] = "aiModified" }));
        });
    }

    [Test]
    public async Task Returns_several_items_and_leaves_out_unknown_keys()
    {
        HttpResponseMessage response = await Client.GetAsync($"{ContentRoot}/items/ai-provenance?id={Page.Key}&id={Guid.NewGuid()}");

        AiProvenanceResponseModel[] items = await ReadAsync<AiProvenanceResponseModel[]>(response);
        Assert.That(items.Select(x => x.Id), Is.EqualTo(new[] { Page.Key }));
    }

    [Test]
    public async Task Returns_not_found_for_an_unknown_key()
    {
        HttpResponseMessage response = await GetItemAsync(Guid.NewGuid());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Treats_an_unpublished_culture_as_not_found_like_umbraco_does()
    {
        ContentService.Unpublish(Page, "da-DK");

        HttpResponseMessage danish = await GetItemAsync(Page.Key, "da-DK");
        HttpResponseMessage english = await GetItemAsync(Page.Key, "en-US");
        var itemsRequest = new HttpRequestMessage(HttpMethod.Get, $"{ContentRoot}/items/ai-provenance?id={Page.Key}");
        itemsRequest.Headers.Add("Accept-Language", "da-DK");
        AiProvenanceResponseModel[] danishItems = await ReadAsync<AiProvenanceResponseModel[]>(await Client.SendAsync(itemsRequest));

        Assert.Multiple(async () =>
        {
            Assert.That(danish.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(english.StatusCode, Is.EqualTo(HttpStatusCode.OK), await english.Content.ReadAsStringAsync());
            Assert.That(danishItems, Is.Empty);
        });
    }

    [Test]
    public async Task Does_not_change_the_content_item_output()
    {
        string body = await Client.GetStringAsync($"{ContentRoot}/item/{Page.Key}");

        Assert.That(body, Does.Not.Contain("aiProvenance"));
    }
}

/// <summary>
/// Checks that the <c>AIProvenance:DeliveryApiEnabled</c> setting turns the endpoints off.
/// </summary>
public sealed class DeliveryApiEndpointDisabledTests : DeliveryApiTestBase
{
    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        base.CustomTestSetup(builder);
        builder.Services.Configure<AiProvenanceSettings>(settings => settings.DeliveryApiEnabled = false);
    }

    [Test]
    public async Task Returns_not_found_when_the_setting_is_off()
    {
        HttpResponseMessage response = await GetItemAsync(Page.Key);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}

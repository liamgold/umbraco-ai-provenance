using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Community.AIProvenance.Api.Models;

namespace Umbraco.Community.AIProvenance.Tests.Api;

/// <summary>
/// Calls the configuration endpoint that tells the backoffice which extra property editors get the mark actions.
/// </summary>
public sealed class ConfigurationEndpointTests : PackageApiTestBase
{
    private const string ConfigurationUrl = $"{ApiRoot}/configuration";

    // Runs while the test server is built, after the package's composer has bound the settings.
    protected override void CustomTestSetup(IUmbracoBuilder builder) =>
        builder.Services.Configure<AiProvenanceSettings>(settings =>
            // Duplicates, spaces and empty entries, as a site's settings might contain them.
            settings.AdditionalPropertyEditorUis =
            [
                "My.PropertyEditorUi.Headline",
                " My.PropertyEditorUi.Headline ",
                "",
                "Umb.PropertyEditorUi.Toggle",
            ]);

    [Test]
    public async Task Returns_the_configured_editors_without_duplicates_or_blanks()
    {
        await AuthenticateClientAsync(Client, $"{Guid.NewGuid():N}@example.com", UserPassword, Umbraco.Cms.Core.Constants.Security.WriterGroupKey);

        HttpResponseMessage response = await Client.GetAsync(ConfigurationUrl);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
        ConfigurationResponseModel? body = await response.Content.ReadFromJsonAsync<ConfigurationResponseModel>();
        Assert.That(body!.AdditionalPropertyEditorUis, Is.EqualTo(new[] { "My.PropertyEditorUi.Headline", "Umb.PropertyEditorUi.Toggle" }));
    }

    [Test]
    public async Task Requires_a_signed_in_backoffice_user()
    {
        HttpResponseMessage response = await Client.GetAsync(ConfigurationUrl);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}

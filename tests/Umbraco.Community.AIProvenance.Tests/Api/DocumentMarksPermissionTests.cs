using System.Net;
using System.Net.Http.Json;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Actions;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;
using Umbraco.Community.AIProvenance.Models;
using Umbraco.Community.AIProvenance.Services;

namespace Umbraco.Community.AIProvenance.Tests.Api;

/// <summary>
/// Calls the document marks endpoints over HTTP as users with different permissions.
/// </summary>
public sealed class DocumentMarksPermissionTests : PackageApiTestBase
{
    private IContentService ContentService => GetRequiredService<IContentService>();

    private IContentTypeService ContentTypeService => GetRequiredService<IContentTypeService>();

    private ILanguageService LanguageService => GetRequiredService<ILanguageService>();

    private IUserGroupService UserGroupService => GetRequiredService<IUserGroupService>();

    private IAiProvenanceService ProvenanceService => GetRequiredService<IAiProvenanceService>();

    private IContent _document = null!;
    private IContent _otherRoot = null!;

    private static string MarksUrl(Guid documentKey) => $"{ApiRoot}/document/{documentKey}/marks";

    [SetUp]
    public async Task CreateContent()
    {
        await LanguageService.CreateAsync(new LanguageBuilder().WithCultureInfo("da-DK").Build(), Umbraco.Cms.Core.Constants.Security.SuperUserKey);

        ContentType contentType = ContentTypeBuilder.CreateBasicContentType(Guid.NewGuid().ToString("N"), "Variant Page");
        contentType.Variations = ContentVariation.Culture;
        contentType.AllowedAsRoot = true;
        ContentTypeService.Save(contentType);

        _document = CreateDocument(contentType);
        _otherRoot = CreateDocument(contentType);
    }

    private IContent CreateDocument(IContentType contentType)
    {
        Content content = ContentBuilder.CreateBasicContent(contentType);
        content.SetCultureName("Page", "en-US");
        content.SetCultureName("Side", "da-DK");
        ContentService.Save(content);
        return content;
    }

    private Task<HttpResponseMessage> PutMarkAsync(string? culture = "en-US") =>
        Client.PutAsJsonAsync(MarksUrl(_document.Key), new
        {
            propertyAlias = "title",
            culture,
            segment = (string?)null,
            elementKey = (Guid?)null,
            origin = "AiGenerated",
        });

    private Task<HttpResponseMessage> GetMarksAsync() => Client.GetAsync(MarksUrl(_document.Key));

    private async Task SignInAsGroupAsync(Guid userGroupKey) =>
        await AuthenticateClientAsync(Client, $"{Guid.NewGuid():N}@example.com", UserPassword, userGroupKey);

    private async Task SignInWithCustomGroupAsync(Action<IUserGroup> configure, ISet<string> permissions)
    {
        var alias = $"group{Guid.NewGuid():N}";
        IUserGroup userGroup = new UserGroupBuilder()
            .WithAlias(alias)
            .WithName(alias)
            .WithAllowedSections([Umbraco.Cms.Core.Constants.Applications.Content])
            .WithPermissions(permissions)
            .Build();
        userGroup.HasAccessToAllLanguages = true;
        configure(userGroup);

        var created = await UserGroupService.CreateAsync(userGroup, Umbraco.Cms.Core.Constants.Security.SuperUserKey);
        Assert.That(created.Success, Is.True, $"Could not create the user group: {created.Status}");
        await SignInAsGroupAsync(created.Result.Key);
    }

    private static async Task AssertStatusAsync(HttpResponseMessage response, HttpStatusCode expected) =>
        Assert.That(response.StatusCode, Is.EqualTo(expected), await response.Content.ReadAsStringAsync());

    [Test]
    public async Task Unauthenticated_requests_are_rejected()
    {
        await AssertStatusAsync(await GetMarksAsync(), HttpStatusCode.Unauthorized);
        await AssertStatusAsync(await PutMarkAsync(), HttpStatusCode.Unauthorized);
    }

    [TestCase("Admin")]
    [TestCase("Editor")]
    [TestCase("Writer")]
    public async Task Users_who_can_update_documents_can_read_and_set_marks(string group)
    {
        await SignInAsGroupAsync(group switch
        {
            "Admin" => Umbraco.Cms.Core.Constants.Security.AdminGroupKey,
            "Editor" => Umbraco.Cms.Core.Constants.Security.EditorGroupKey,
            _ => Umbraco.Cms.Core.Constants.Security.WriterGroupKey,
        });

        await AssertStatusAsync(await PutMarkAsync(), HttpStatusCode.OK);
        await AssertStatusAsync(await GetMarksAsync(), HttpStatusCode.OK);
        Assert.That(
            await ProvenanceService.GetMarkAsync(new AiPropertyReference(_document.Key, "title", "en-US"), AiProvenanceState.Draft),
            Is.Not.Null);
    }

    [Test]
    public async Task Translators_without_the_content_section_are_forbidden()
    {
        await SignInAsGroupAsync(Umbraco.Cms.Core.Constants.Security.TranslatorGroupKey);

        await AssertStatusAsync(await GetMarksAsync(), HttpStatusCode.Forbidden);
        await AssertStatusAsync(await PutMarkAsync(), HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Users_who_can_only_browse_can_read_but_not_set_marks()
    {
        await SignInWithCustomGroupAsync(_ => { }, new HashSet<string> { ActionBrowse.ActionLetter });

        await AssertStatusAsync(await GetMarksAsync(), HttpStatusCode.OK);
        await AssertStatusAsync(await PutMarkAsync(), HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Users_outside_the_documents_start_node_cannot_read_or_set_marks()
    {
        await SignInWithCustomGroupAsync(
            group => group.StartContentId = _otherRoot.Id,
            new HashSet<string> { ActionBrowse.ActionLetter, ActionUpdate.ActionLetter });

        await AssertStatusAsync(await GetMarksAsync(), HttpStatusCode.Forbidden);
        await AssertStatusAsync(await PutMarkAsync(), HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Users_without_access_to_a_language_cannot_set_marks_in_it()
    {
        ILanguage english = (await LanguageService.GetAsync("en-US"))!;
        await SignInWithCustomGroupAsync(
            group =>
            {
                group.HasAccessToAllLanguages = false;
                group.AddAllowedLanguage(english.Id);
            },
            new HashSet<string> { ActionBrowse.ActionLetter, ActionUpdate.ActionLetter });

        await AssertStatusAsync(await PutMarkAsync("en-US"), HttpStatusCode.OK);
        await AssertStatusAsync(await PutMarkAsync("da-DK"), HttpStatusCode.Forbidden);
    }
}

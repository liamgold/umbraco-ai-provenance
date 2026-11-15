using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Umbraco.AI.Core.EntityAdapter;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Community.AIProvenance.Models;
using Umbraco.Community.AIProvenance.UmbracoAI;
using Umbraco.Community.AIProvenance.UmbracoAI.Proposals;
using Umbraco.Extensions;
using UmbracoAIConstants = Umbraco.AI.Core.Constants;

namespace Umbraco.Community.AIProvenance.Tests.UmbracoAI;

/// <summary>
/// Tests marking values that Umbraco.AI's Copilot proposes with its <c>set_value</c> browser tool, which the editor
/// then saves outside the agent run.
/// </summary>
[TestFixture]
public sealed class CopilotProposalTests
{
    private static readonly Guid DocumentKey = Guid.NewGuid();
    private static readonly Guid Editor = Guid.NewGuid();

    private static FunctionCallContent SetValue(object path, object value, string? culture = null) =>
        new("call-1", SetValueProposalChatMiddleware.SetValueToolName, new Dictionary<string, object?>
        {
            ["path"] = JsonSerializer.SerializeToElement(path),
            ["value"] = JsonSerializer.SerializeToElement(value),
            ["culture"] = culture is null ? null : JsonSerializer.SerializeToElement(culture),
        });

    /// <summary>A Copilot run on a document, as Umbraco.AI sets up its runtime context.</summary>
    private static AIRuntimeContext CopilotRun(string? viewedCulture = null)
    {
        var context = new AIRuntimeContext([]);
        context.SetValue(UmbracoAIConstants.ContextKeys.EntityType, "document");
        context.SetValue(UmbracoAIConstants.ContextKeys.EntityId, DocumentKey);
        context.SetValue(UmbracoAIConstants.ContextKeys.ProviderId, "microsoft-foundry");
        context.SetValue(UmbracoAIConstants.ContextKeys.ModelId, "gpt-test");
        context.SetValue(UmbracoAIConstants.ContextKeys.SerializedEntity, new AISerializedEntity
        {
            EntityType = "document",
            Unique = DocumentKey.ToString(),
            Name = "Page",
            Culture = viewedCulture,
            Data = JsonSerializer.SerializeToElement(new { }),
        });
        return context;
    }

    [TestCase("Plain text", "  Plain   text ")]
    [TestCase("<p>Rich <strong>text</strong> &amp; more</p>", "Rich text & more")]
    [TestCase("{\"markup\":\"<p>Stored rich text</p>\",\"blocks\":null}", "Stored rich text")]
    [TestCase("<p>Written by <em>AI</em>.</p>", "Written by AI.")]
    [TestCase("<p>One</p><p>Two</p>", "One Two")]
    public void Values_are_compared_as_text(string stored, string proposed) =>
        Assert.That(ValueText.Normalize(stored), Is.EqualTo(ValueText.Normalize(proposed)));

    [Test]
    public void A_value_inside_a_block_is_found_by_the_block_key()
    {
        var elementKey = Guid.NewGuid();
        string blockValue = JsonSerializer.Serialize(new
        {
            layout = new { },
            contentData = new[]
            {
                new { key = Guid.NewGuid(), values = new[] { new { alias = "headline", value = "Other block" } } },
                new { key = elementKey, values = new[] { new { alias = "headline", value = "Written by AI" } } },
            },
        });

        Assert.That(ValueText.FindBlockValue(blockValue, elementKey, "headline"), Is.EqualTo("Written by AI"));
        Assert.That(ValueText.FindBlockValue(blockValue, Guid.NewGuid(), "headline"), Is.Null);
    }

    [Test]
    public void A_set_value_call_becomes_a_proposal_for_the_open_document()
    {
        AiValueProposal? proposal = SetValueProposalChatMiddleware.CreateProposal(
            SetValue(new[] { "title" }, "A title by AI"), CopilotRun(viewedCulture: "en-US"), Editor);

        Assert.That(proposal, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(proposal!.DocumentKey, Is.EqualTo(DocumentKey));
            Assert.That(proposal.UserKey, Is.EqualTo(Editor));
            Assert.That(proposal.PropertyAlias, Is.EqualTo("title"));
            Assert.That(proposal.ElementKey, Is.Null);
            Assert.That(proposal.Culture, Is.EqualTo("en-US"), "Without a culture, the Copilot edits the culture being viewed.");
            Assert.That(proposal.Value, Is.EqualTo("A title by AI"));
            Assert.That(proposal.Provider, Is.EqualTo("microsoft-foundry"));
            Assert.That(proposal.Model, Is.EqualTo("gpt-test"));
        });
    }

    [Test]
    public void A_set_value_call_inside_a_block_targets_the_block_property()
    {
        var blockKey = Guid.NewGuid();
        AiValueProposal? proposal = SetValueProposalChatMiddleware.CreateProposal(
            SetValue(new object[] { "contentBlocks", new { blockKey }, "headline" }, "Block headline"), CopilotRun(), Editor);

        Assert.That(proposal, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(proposal!.RootPropertyAlias, Is.EqualTo("contentBlocks"));
            Assert.That(proposal.PropertyAlias, Is.EqualTo("headline"));
            Assert.That(proposal.ElementKey, Is.EqualTo(blockKey));
        });
    }

    [Test]
    public void Calls_that_are_not_about_a_document_property_are_ignored()
    {
        var mediaRun = CopilotRun();
        mediaRun.SetValue(UmbracoAIConstants.ContextKeys.EntityType, "media");

        Assert.Multiple(() =>
        {
            Assert.That(SetValueProposalChatMiddleware.CreateProposal(SetValue(new[] { "title" }, "x"), mediaRun, Editor), Is.Null, "Not a document.");
            Assert.That(SetValueProposalChatMiddleware.CreateProposal(SetValue(new[] { "title" }, "x"), null, Editor), Is.Null, "No AI run.");
            Assert.That(SetValueProposalChatMiddleware.CreateProposal(SetValue(new[] { "title" }, "x"), CopilotRun(), null), Is.Null, "No user.");
            Assert.That(SetValueProposalChatMiddleware.CreateProposal(SetValue(new[] { "title" }, " "), CopilotRun(), Editor), Is.Null, "An empty value.");
            Assert.That(SetValueProposalChatMiddleware.CreateProposal(SetValue(new[] { "a", "b" }, "x"), CopilotRun(), Editor), Is.Null, "A malformed path.");
        });
    }

    [Test]
    public async Task The_middleware_remembers_a_set_value_call_in_a_streamed_response()
    {
        var store = new AiValueProposalStore(new AppCaches(NoAppCache.Instance, NoAppCache.Instance, new IsolatedCaches(_ => new ObjectCacheAppCache())));
        var accessor = new FakeRuntimeContextAccessor { Context = CopilotRun() };
        var middleware = new SetValueProposalChatMiddleware(
            accessor,
            BackOfficeUser(Editor),
            store,
            Mock.Of<IOptionsMonitor<AiProvenanceUmbracoAISettings>>(x => x.CurrentValue == new AiProvenanceUmbracoAISettings()),
            NullLogger<SetValueProposalChatMiddleware>.Instance);

        IChatClient client = middleware.Apply(new StreamingClient(new ChatResponseUpdate(ChatRole.Assistant, [SetValue(new[] { "title" }, "Streamed title")])));
        await foreach (ChatResponseUpdate _ in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "Write a title")]))
        {
        }

        Assert.That(store.Get(Editor, DocumentKey).Select(x => x.Value), Is.EqualTo(new[] { "Streamed title" }));
    }

    internal static IBackOfficeSecurityAccessor BackOfficeUser(Guid userKey)
    {
        var user = Mock.Of<IUser>(x => x.Key == userKey);
        var security = Mock.Of<IBackOfficeSecurity>(x => x.CurrentUser == user);
        return Mock.Of<IBackOfficeSecurityAccessor>(x => x.BackOfficeSecurity == security);
    }

    internal sealed class FakeRuntimeContextAccessor : IAIRuntimeContextAccessor
    {
        public AIRuntimeContext? Context { get; set; }
    }

    /// <summary>A model that streams fixed updates.</summary>
    private sealed class StreamingClient(params ChatResponseUpdate[] updates) : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, updates.SelectMany(x => x.Contents).ToList())));

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (ChatResponseUpdate update in updates)
            {
                await Task.Yield();
                yield return update;
            }
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }
}

/// <summary>
/// Tests the editor's save after the Copilot proposed values, against real Umbraco content.
/// </summary>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest, WithApplication = true)]
public sealed class CopilotProposalSaveTests : AiProvenanceTestBase
{
    private AiValueProposalStore Store => GetRequiredService<AiValueProposalStore>();

    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        base.CustomTestSetup(builder);
        new AiProvenanceUmbracoAIComposer().Compose(builder);

        // Integration tests turn caching off, and the proposals live in a cache. Give only the store a real cache:
        // Umbraco's own caches would hand back the content being edited instead of a copy of the saved version.
        builder.Services.AddUnique(new AiValueProposalStore(
            new AppCaches(NoAppCache.Instance, NoAppCache.Instance, new IsolatedCaches(_ => new ObjectCacheAppCache()))));

        // The editor saves outside any AI run, as the signed-in backoffice user.
        builder.Services.RemoveAll<IAIRuntimeContextAccessor>();
        builder.Services.AddSingleton<IAIRuntimeContextAccessor>(new CopilotProposalTests.FakeRuntimeContextAccessor());
        builder.Services.RemoveAll<IBackOfficeSecurityAccessor>();
        builder.Services.AddSingleton(CopilotProposalTests.BackOfficeUser(UserKey));
    }

    private void Propose(string alias, string value, Guid? userKey = null) =>
        Store.Add(new AiValueProposal(userKey ?? UserKey, Textpage.Key, alias, alias, null, null, null, ValueText.Normalize(value), "microsoft-foundry", "gpt-test"));

    private Task<AiProvenanceMark?> DraftMark(string alias) =>
        ProvenanceService.GetMarkAsync(new AiPropertyReference(Textpage.Key, alias), AiProvenanceState.Draft);

    [Test]
    public async Task Saving_the_proposed_value_marks_the_property_with_the_model()
    {
        Propose("title", "A title by AI");

        Textpage.SetValue("title", "A title by AI");
        ContentService.Save(Textpage);

        AiProvenanceMark? mark = await DraftMark("title");
        Assert.That(mark, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(mark!.Origin, Is.EqualTo(AiOrigin.AiModified));
            Assert.That(mark.Source, Is.EqualTo(AiProvenanceSource.UmbracoAI));
            Assert.That(mark.AiModel, Is.EqualTo("gpt-test"));
            Assert.That(Store.Get(UserKey, Textpage.Key), Is.Empty, "A used proposal is forgotten.");
        });
    }

    [Test]
    public async Task Saving_proposed_rich_text_matches_on_its_text()
    {
        Textpage.SetValue("bodyText", string.Empty);
        ContentService.Save(Textpage);
        Propose("bodyText", "A summary written by AI.");

        Textpage.SetValue("bodyText", "{\"markup\":\"<p>A summary written by <em>AI</em>.</p>\",\"blocks\":null}");
        ContentService.Save(Textpage);

        Assert.That((await DraftMark("bodyText"))?.Origin, Is.EqualTo(AiOrigin.AiGenerated), "The value was empty before.");
    }

    [Test]
    public async Task A_value_the_editor_rewrote_is_not_marked()
    {
        Propose("title", "A title by AI");

        Textpage.SetValue("title", "A title the editor rewrote");
        ContentService.Save(Textpage);

        Assert.That(await DraftMark("title"), Is.Null);
        Assert.That(Store.Get(UserKey, Textpage.Key), Has.Count.EqualTo(1), "The proposal waits in case the editor saves it later.");
    }

    [Test]
    public async Task A_proposal_to_another_editor_is_not_used()
    {
        Propose("title", "A title by AI", userKey: Guid.NewGuid());

        Textpage.SetValue("title", "A title by AI");
        ContentService.Save(Textpage);

        Assert.That(await DraftMark("title"), Is.Null);
    }
}

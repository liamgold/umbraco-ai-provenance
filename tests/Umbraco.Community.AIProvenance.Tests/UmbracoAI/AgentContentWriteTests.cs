using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Community.AIProvenance.Models;
using Umbraco.Community.AIProvenance.UmbracoAI;
using UmbracoAIConstants = Umbraco.AI.Core.Constants;

namespace Umbraco.Community.AIProvenance.Tests.UmbracoAI;

/// <summary>
/// Tests automatic marking with a simulated Umbraco.AI agent run, without calling an AI model.
/// </summary>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest, WithApplication = true)]
public sealed class AgentContentWriteTests : AiProvenanceTestBase
{
    private readonly FakeRuntimeContextAccessor _runtimeContextAccessor = new();

    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        base.CustomTestSetup(builder);
        new AiProvenanceUmbracoAIComposer().Compose(builder);
        builder.Services.RemoveAll<IAIRuntimeContextAccessor>();
        builder.Services.AddSingleton<IAIRuntimeContextAccessor>(_runtimeContextAccessor);
    }

    [TearDown]
    public void EndAgentRun() => _runtimeContextAccessor.Context = null;

    /// <summary>Simulates an agent run, as Umbraco.AI sets it up before it calls a model.</summary>
    private void StartAgentRun(string provider = "microsoft-foundry", string model = "test-model")
    {
        var context = new AIRuntimeContext([]);
        context.SetValue(UmbracoAIConstants.ContextKeys.ProviderId, provider);
        context.SetValue(UmbracoAIConstants.ContextKeys.ModelId, model);
        _runtimeContextAccessor.Context = context;
    }

    private Task<AiProvenanceMark?> DraftMark(string alias) =>
        ProvenanceService.GetMarkAsync(new AiPropertyReference(Textpage.Key, alias), AiProvenanceState.Draft);

    [Test]
    public async Task A_save_outside_an_agent_run_sets_no_marks()
    {
        Textpage.SetValue("title", "Changed by a person");
        ContentService.Save(Textpage);

        Assert.That(await ProvenanceService.GetMarksAsync(Textpage.Key, AiProvenanceState.Draft), Is.Empty);
    }

    [Test]
    public async Task An_agent_changing_a_value_marks_it_ai_modified_with_the_model()
    {
        StartAgentRun(model: "gpt-test");
        Textpage.SetValue("title", "Changed by an agent");
        ContentService.Save(Textpage);

        AiProvenanceMark? mark = await DraftMark("title");
        Assert.That(mark, Is.Not.Null);
        Assert.That(mark!.Origin, Is.EqualTo(AiOrigin.AiModified));
        Assert.That(mark.Source, Is.EqualTo(AiProvenanceSource.UmbracoAI));
        Assert.That(mark.AiProvider, Is.EqualTo("microsoft-foundry"));
        Assert.That(mark.AiModel, Is.EqualTo("gpt-test"));
    }

    [Test]
    public async Task An_agent_filling_an_empty_value_marks_it_ai_generated()
    {
        Textpage.SetValue("author", string.Empty);
        ContentService.Save(Textpage);

        StartAgentRun();
        Textpage.SetValue("author", "Written by an agent");
        ContentService.Save(Textpage);

        Assert.That((await DraftMark("author"))!.Origin, Is.EqualTo(AiOrigin.AiGenerated));
    }

    [Test]
    public async Task Unchanged_values_are_not_marked()
    {
        StartAgentRun();
        Textpage.SetValue("title", "Changed by an agent");
        ContentService.Save(Textpage);

        Assert.That(await DraftMark("bodyText"), Is.Null);
        Assert.That(await DraftMark("author"), Is.Null);
    }

    [Test]
    public async Task An_ai_generated_mark_stays_ai_generated()
    {
        await ProvenanceService.SetMarkAsync(
            new AiPropertyReference(Textpage.Key, "title"), AiOrigin.AiGenerated, AiProvenanceSource.Manual, UserKey);

        StartAgentRun();
        Textpage.SetValue("title", "Changed again by an agent");
        ContentService.Save(Textpage);

        Assert.That((await DraftMark("title"))!.Origin, Is.EqualTo(AiOrigin.AiGenerated));
    }

    private sealed class FakeRuntimeContextAccessor : IAIRuntimeContextAccessor
    {
        public AIRuntimeContext? Context { get; set; }
    }
}

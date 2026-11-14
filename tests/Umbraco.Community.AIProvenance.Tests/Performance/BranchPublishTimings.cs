using System.Diagnostics;
using NUnit.Framework;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Community.AIProvenance.Models;
using Umbraco.Community.AIProvenance.Services;

namespace Umbraco.Community.AIProvenance.Tests.Performance;

/// <summary>
/// Times publishing and deleting a large branch, to show what the package adds to those operations.
/// </summary>
/// <remarks>
/// Run on request only: <c>dotnet test --filter "Category=Performance"</c>. The same steps run with and without
/// the package, so the difference between the two fixtures is the package's cost. Set
/// <c>Tests__Database__DatabaseType=LocalDb</c> to time SQL Server instead of SQLite.
/// </remarks>
internal static class BranchPublishSteps
{
    public const int Children = 1000;

    /// <summary>Every tenth child gets a mark.</summary>
    public const int MarkEvery = 10;

    public static List<IContent> CreateChildren(IContentService contentService, IContentType contentType, IContent root)
    {
        var children = new List<IContent>(Children);
        for (var i = 0; i < Children; i++)
        {
            Content child = ContentBuilder.CreateSimpleContent(contentType, $"Child {i}", root);
            contentService.Save(child);
            children.Add(child);
        }

        return children;
    }

    /// <param name="afterPublish">Runs after both publishes and before the delete, outside the timings.</param>
    public static void Run(IContentService contentService, IContent root, string label, Action? afterPublish = null)
    {
        TimeSpan firstPublish = Time(() => contentService.PublishBranch(root, PublishBranchFilter.IncludeUnpublished, ["*"]));
        TimeSpan republish = Time(() => contentService.PublishBranch(root, PublishBranchFilter.ForceRepublish, ["*"]));
        afterPublish?.Invoke();
        TimeSpan delete = Time(() => contentService.Delete(root));

        TestContext.Progress.WriteLine(
            $"{label}: first publish {firstPublish.TotalMilliseconds:N0} ms, republish {republish.TotalMilliseconds:N0} ms, " +
            $"delete {delete.TotalMilliseconds:N0} ms ({Children + 1} documents)");
    }

    private static TimeSpan Time(Action action)
    {
        var stopwatch = Stopwatch.StartNew();
        action();
        return stopwatch.Elapsed;
    }
}

[TestFixture]
[Explicit("Timing test. Run on request.")]
[Category("Performance")]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest, WithApplication = true)]
public sealed class BranchPublishWithPackageTimings : AiProvenanceTestBase
{
    [Test]
    public async Task Publish_and_delete_a_branch_with_marks()
    {
        List<IContent> children = BranchPublishSteps.CreateChildren(ContentService, ContentType, Textpage);
        for (var i = 0; i < children.Count; i += BranchPublishSteps.MarkEvery)
        {
            await ProvenanceService.SetMarkAsync(
                new AiPropertyReference(children[i].Key, "title"), AiOrigin.AiGenerated, AiProvenanceSource.Manual, UserKey);
        }

        // Check that publishing the branch published the marks, so the timing covers the real work.
        BranchPublishSteps.Run(ContentService, Textpage, "With the package", () =>
        {
            int published = children
                .Where((_, i) => i % BranchPublishSteps.MarkEvery == 0)
                .Sum(child => ProvenanceService.GetMarksAsync(child.Key, AiProvenanceState.Published).GetAwaiter().GetResult().Count);
            Assert.That(published, Is.EqualTo(BranchPublishSteps.Children / BranchPublishSteps.MarkEvery));
        });

        Assert.That(await ProvenanceService.GetMarksAsync(children[0].Key, AiProvenanceState.Draft), Is.Empty, "Deleting removed the marks.");
    }
}

[TestFixture]
[Explicit("Timing test. Run on request.")]
[Category("Performance")]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest, WithApplication = true)]
public sealed class BranchPublishWithoutPackageTimings : UmbracoIntegrationTestWithContent
{
    [Test]
    public void Publish_and_delete_a_branch()
    {
        BranchPublishSteps.CreateChildren(ContentService, ContentType, Textpage);
        BranchPublishSteps.Run(ContentService, Textpage, "Without the package");
    }
}

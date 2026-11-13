using NUnit.Framework;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Community.AIProvenance.Models;
using Umbraco.Community.AIProvenance.Notifications;
using Umbraco.Extensions;

namespace Umbraco.Community.AIProvenance.Tests;

/// <summary>
/// Tests that setting and removing marks publishes the package's notifications.
/// </summary>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public sealed class MarkNotificationTests : AiProvenanceTestBase
{
    private static readonly List<INotification> Received = [];

    private static readonly AiPropertyReference Summary =
        new(Guid.Parse("4b9f7d0e-1c2a-4e3b-8f5d-6a7b8c9d0e1f"), "summary");

    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        base.CustomTestSetup(builder);
        builder
            .AddNotificationHandler<AiProvenanceMarkSavedNotification, RecordingHandler>()
            .AddNotificationHandler<AiProvenanceMarkRemovedNotification, RecordingHandler>();
    }

    [SetUp]
    public void ClearReceived() => Received.Clear();

    private Task Mark(AiOrigin origin) =>
        ProvenanceService.SetMarkAsync(Summary, origin, AiProvenanceSource.UmbracoAI, UserKey, "microsoft-foundry", "gpt-test");

    [Test]
    public async Task Setting_a_mark_publishes_saved_with_no_previous_origin()
    {
        await Mark(AiOrigin.AiGenerated);

        var saved = Received.OfType<AiProvenanceMarkSavedNotification>().Single();
        Assert.That(saved.PreviousOrigin, Is.EqualTo(AiOrigin.None));
        Assert.That(saved.Mark.Origin, Is.EqualTo(AiOrigin.AiGenerated));
        Assert.That(saved.Mark.Property.PropertyAlias, Is.EqualTo("summary"));
        Assert.That(saved.Mark.Source, Is.EqualTo(AiProvenanceSource.UmbracoAI));
        Assert.That(saved.Mark.AiModel, Is.EqualTo("gpt-test"));
        Assert.That(saved.Mark.UpdatedBy, Is.EqualTo(UserKey));
    }

    [Test]
    public async Task Changing_a_mark_publishes_saved_with_the_previous_origin()
    {
        await Mark(AiOrigin.AiGenerated);
        Received.Clear();

        await Mark(AiOrigin.AiModified);

        var saved = Received.OfType<AiProvenanceMarkSavedNotification>().Single();
        Assert.That(saved.PreviousOrigin, Is.EqualTo(AiOrigin.AiGenerated));
        Assert.That(saved.Mark.Origin, Is.EqualTo(AiOrigin.AiModified));
    }

    [Test]
    public async Task Removing_a_mark_publishes_removed_with_the_old_mark()
    {
        await Mark(AiOrigin.AiModified);
        Received.Clear();

        await ProvenanceService.RemoveMarkAsync(Summary);

        var removed = Received.OfType<AiProvenanceMarkRemovedNotification>().Single();
        Assert.That(removed.Mark.Origin, Is.EqualTo(AiOrigin.AiModified));
        Assert.That(Received.OfType<AiProvenanceMarkSavedNotification>(), Is.Empty);
    }

    [Test]
    public async Task Setting_none_publishes_removed()
    {
        await Mark(AiOrigin.AiGenerated);
        Received.Clear();

        await Mark(AiOrigin.None);

        Assert.That(Received.OfType<AiProvenanceMarkRemovedNotification>(), Has.Exactly(1).Items);
    }

    [Test]
    public async Task Removing_a_property_with_no_mark_publishes_nothing()
    {
        await ProvenanceService.RemoveMarkAsync(Summary);

        Assert.That(Received, Is.Empty);
    }

    [Test]
    public async Task Publishing_a_document_does_not_publish_mark_notifications()
    {
        await Mark(AiOrigin.AiGenerated);
        Received.Clear();

        await ProvenanceService.PublishAsync(Summary.DocumentKey, cultures: null);

        Assert.That(Received, Is.Empty);
    }

    private sealed class RecordingHandler :
        INotificationHandler<AiProvenanceMarkSavedNotification>,
        INotificationHandler<AiProvenanceMarkRemovedNotification>
    {
        public void Handle(AiProvenanceMarkSavedNotification notification) => Received.Add(notification);

        public void Handle(AiProvenanceMarkRemovedNotification notification) => Received.Add(notification);
    }
}

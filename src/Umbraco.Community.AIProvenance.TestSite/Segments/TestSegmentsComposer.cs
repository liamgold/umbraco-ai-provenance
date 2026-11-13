using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;
using Umbraco.Extensions;

namespace Umbraco.Community.AIProvenance.TestSite.Segments;

/// <summary>
/// Turns on segments in the test site, with two test segments, so marks on segment variants can be tested.
/// </summary>
/// <remarks>
/// Umbraco 17 does not read segment settings from configuration. A site enables segments in code and provides an
/// <see cref="ISegmentService"/> that lists them.
/// </remarks>
public sealed class TestSegmentsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.Configure<SegmentSettings>(settings =>
        {
            settings.Enabled = true;
            settings.AllowCreation = false;
        });
        builder.Services.AddUnique<ISegmentService, TestSegmentService>();
    }

    private sealed class TestSegmentService : ISegmentService
    {
        private static readonly Segment[] Segments =
        [
            new() { Alias = "mobile", Name = "Mobile visitors" },
            new() { Alias = "returning", Name = "Returning visitors" },
        ];

        public Task<Attempt<PagedModel<Segment>?, SegmentOperationStatus>> GetPagedSegmentsAsync(int skip = 0, int take = 100) =>
            Task.FromResult(Attempt.SucceedWithStatus<PagedModel<Segment>?, SegmentOperationStatus>(
                SegmentOperationStatus.Success,
                new PagedModel<Segment> { Total = Segments.Length, Items = Segments.Skip(skip).Take(take) }));
    }
}

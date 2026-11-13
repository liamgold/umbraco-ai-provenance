using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Infrastructure.Serialization;
using Umbraco.Cms.Infrastructure.Sync;

namespace Umbraco.Community.AIProvenance.Tests.Api;

/// <summary>
/// Runs cache refreshes on this server straight away, so content published in a test reaches the published cache.
/// </summary>
/// <remarks>
/// The test server's own messenger ignores cache refreshes. Running them locally needs no other server,
/// so the remote methods do nothing.
/// </remarks>
internal sealed class LocalServerMessenger : ServerMessengerBase
{
    public LocalServerMessenger()
        : base(false, new SystemTextJsonSerializer(new DefaultJsonSerializerEncoderFactory()))
    {
    }

    public override void SendMessages()
    {
    }

    public override void Sync()
    {
    }

    protected override void DeliverRemote(ICacheRefresher refresher, MessageType messageType, IEnumerable<object>? ids = null, string? json = null)
    {
    }
}

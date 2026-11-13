using Umbraco.Cms.Core.Scoping;
using Umbraco.Community.AIProvenance.Models;
using Umbraco.Community.AIProvenance.Notifications;
using Umbraco.Community.AIProvenance.Persistence;

namespace Umbraco.Community.AIProvenance.Services;

internal sealed class AiProvenanceService : IAiProvenanceService
{
    private readonly ICoreScopeProvider _scopeProvider;
    private readonly AiProvenanceRepository _repository;
    private readonly TimeProvider _timeProvider;

    public AiProvenanceService(ICoreScopeProvider scopeProvider, AiProvenanceRepository repository, TimeProvider timeProvider)
    {
        _scopeProvider = scopeProvider;
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public Task<IReadOnlyCollection<AiProvenanceMark>> GetMarksAsync(Guid documentKey, AiProvenanceState state)
    {
        using ICoreScope scope = _scopeProvider.CreateCoreScope(autoComplete: true);
        IReadOnlyCollection<AiProvenanceMark> marks = _repository.GetForDocument(documentKey)
            .Where(x => x.State == (byte)state)
            .Select(ToMark)
            .ToArray();
        return Task.FromResult(marks);
    }

    public Task<AiProvenanceMark?> GetMarkAsync(AiPropertyReference property, AiProvenanceState state)
    {
        property = property.Normalize();
        using ICoreScope scope = _scopeProvider.CreateCoreScope(autoComplete: true);
        AiProvenanceDto? row = _repository.GetForDocument(property.DocumentKey)
            .FirstOrDefault(x => x.State == (byte)state && Matches(x, property));
        return Task.FromResult(row is null ? null : ToMark(row));
    }

    public Task SetMarkAsync(
        AiPropertyReference property,
        AiOrigin origin,
        AiProvenanceSource source,
        Guid userKey,
        string? aiProvider = null,
        string? aiModel = null)
    {
        if (origin == AiOrigin.None)
        {
            return RemoveMarkAsync(property);
        }

        property = property.Normalize();
        using ICoreScope scope = _scopeProvider.CreateCoreScope();

        AiProvenanceDto[] existing = _repository.GetForDocument(property.DocumentKey)
            .Where(x => x.State == (byte)AiProvenanceState.Draft && Matches(x, property))
            .ToArray();
        _repository.Delete(existing.Select(x => x.Id));

        var row = new AiProvenanceDto
        {
            DocumentKey = property.DocumentKey,
            ElementKey = property.ElementKey,
            PropertyAlias = property.PropertyAlias,
            Culture = property.Culture,
            Segment = property.Segment,
            State = (byte)AiProvenanceState.Draft,
            Origin = (byte)origin,
            Source = (byte)source,
            AiProvider = aiProvider,
            AiModel = aiModel,
            UpdatedBy = userKey,
            UpdatedUtc = _timeProvider.GetUtcNow().UtcDateTime,
        };
        _repository.Insert([row]);

        // The scope publishes the notification when it completes, so handlers never see a rolled-back change.
        AiOrigin previousOrigin = existing.Length > 0 ? (AiOrigin)existing[0].Origin : AiOrigin.None;
        scope.Notifications.Publish(new AiProvenanceMarkSavedNotification(ToMark(row), previousOrigin));

        scope.Complete();
        return Task.CompletedTask;
    }

    public Task RemoveMarkAsync(AiPropertyReference property)
    {
        property = property.Normalize();
        using ICoreScope scope = _scopeProvider.CreateCoreScope();

        AiProvenanceDto[] existing = _repository.GetForDocument(property.DocumentKey)
            .Where(x => x.State == (byte)AiProvenanceState.Draft && Matches(x, property))
            .ToArray();
        _repository.Delete(existing.Select(x => x.Id));

        if (existing.Length > 0)
        {
            scope.Notifications.Publish(new AiProvenanceMarkRemovedNotification(ToMark(existing[0])));
        }

        scope.Complete();
        return Task.CompletedTask;
    }

    public Task PublishAsync(Guid documentKey, IEnumerable<string>? cultures)
    {
        Func<string?, bool> inScope = CultureFilter(cultures);
        using ICoreScope scope = _scopeProvider.CreateCoreScope();

        List<AiProvenanceDto> rows = _repository.GetForDocument(documentKey);

        // Invariant values (a null culture) are published with every culture.
        _repository.Delete(rows
            .Where(x => x.State == (byte)AiProvenanceState.Published && (x.Culture is null || inScope(x.Culture)))
            .Select(x => x.Id));

        _repository.Insert(rows
            .Where(x => x.State == (byte)AiProvenanceState.Draft && (x.Culture is null || inScope(x.Culture)))
            .Select(x => Clone(x, documentKey, AiProvenanceState.Published)));

        scope.Complete();
        return Task.CompletedTask;
    }

    public Task UnpublishAsync(Guid documentKey, IEnumerable<string>? cultures)
    {
        Func<string?, bool> inScope = CultureFilter(cultures);
        bool wholeDocument = cultures is null || cultures.Any(c => c == "*");
        using ICoreScope scope = _scopeProvider.CreateCoreScope();

        // When one culture is unpublished, the document stays published, so invariant values keep their marks.
        _repository.Delete(_repository.GetForDocument(documentKey)
            .Where(x => x.State == (byte)AiProvenanceState.Published
                && (wholeDocument || (x.Culture is not null && inScope(x.Culture))))
            .Select(x => x.Id));

        scope.Complete();
        return Task.CompletedTask;
    }

    public Task CopyAsync(Guid sourceDocumentKey, Guid targetDocumentKey)
    {
        using ICoreScope scope = _scopeProvider.CreateCoreScope();

        _repository.Delete(_repository.GetForDocument(targetDocumentKey)
            .Where(x => x.State == (byte)AiProvenanceState.Draft)
            .Select(x => x.Id));

        _repository.Insert(_repository.GetForDocument(sourceDocumentKey)
            .Where(x => x.State == (byte)AiProvenanceState.Draft && x.ElementKey is null)
            .Select(x => Clone(x, targetDocumentKey, AiProvenanceState.Draft)));

        scope.Complete();
        return Task.CompletedTask;
    }

    public Task DeleteAllAsync(Guid documentKey)
    {
        using ICoreScope scope = _scopeProvider.CreateCoreScope();
        _repository.DeleteForDocument(documentKey);
        scope.Complete();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns a test for whether a stored culture is in the given cultures. Null or "*" means every culture.
    /// </summary>
    private static Func<string?, bool> CultureFilter(IEnumerable<string>? cultures)
    {
        if (cultures is null)
        {
            return _ => true;
        }

        string[] list = cultures.ToArray();
        if (list.Any(c => c == "*"))
        {
            return _ => true;
        }

        var set = new HashSet<string>(
            list.Select(AiPropertyReference.NormalizeCulture).OfType<string>(),
            StringComparer.Ordinal);
        return culture => culture is not null && set.Contains(culture);
    }

    private static bool Matches(AiProvenanceDto row, AiPropertyReference property) =>
        row.ElementKey == property.ElementKey
        && string.Equals(row.PropertyAlias, property.PropertyAlias, StringComparison.OrdinalIgnoreCase)
        && string.Equals(row.Culture, property.Culture, StringComparison.Ordinal)
        && string.Equals(row.Segment, property.Segment, StringComparison.Ordinal);

    private static AiProvenanceDto Clone(AiProvenanceDto row, Guid documentKey, AiProvenanceState state) => new()
    {
        DocumentKey = documentKey,
        ElementKey = row.ElementKey,
        PropertyAlias = row.PropertyAlias,
        Culture = row.Culture,
        Segment = row.Segment,
        State = (byte)state,
        Origin = row.Origin,
        Source = row.Source,
        AiProvider = row.AiProvider,
        AiModel = row.AiModel,
        UpdatedBy = row.UpdatedBy,
        UpdatedUtc = row.UpdatedUtc,
    };

    private static AiProvenanceMark ToMark(AiProvenanceDto row) => new()
    {
        Property = new AiPropertyReference(row.DocumentKey, row.PropertyAlias, row.Culture, row.Segment, row.ElementKey),
        State = (AiProvenanceState)row.State,
        Origin = (AiOrigin)row.Origin,
        Source = (AiProvenanceSource)row.Source,
        AiProvider = row.AiProvider,
        AiModel = row.AiModel,
        UpdatedBy = row.UpdatedBy,
        UpdatedUtc = DateTime.SpecifyKind(row.UpdatedUtc, DateTimeKind.Utc),
    };
}

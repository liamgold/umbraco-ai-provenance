using NPoco;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;

namespace Umbraco.Community.AIProvenance.Persistence;

/// <summary>
/// Reads and writes rows of the <c>aiProvenance</c> table. Call it inside a scope.
/// </summary>
internal sealed class AiProvenanceRepository
{
    private readonly IScopeAccessor _scopeAccessor;

    public AiProvenanceRepository(IScopeAccessor scopeAccessor) => _scopeAccessor = scopeAccessor;

    private IUmbracoDatabase Database =>
        _scopeAccessor.AmbientScope?.Database
        ?? throw new InvalidOperationException("The AI provenance repository needs an ambient scope.");

    /// <summary>
    /// Gets a document's rows. A document has few marks, so callers filter the result in memory.
    /// </summary>
    public List<AiProvenanceDto> GetForDocument(Guid documentKey)
    {
        Sql<ISqlContext> sql = Database.SqlContext.Sql()
            .SelectAll()
            .From<AiProvenanceDto>()
            .Where<AiProvenanceDto>(x => x.DocumentKey == documentKey);

        return Database.Fetch<AiProvenanceDto>(sql);
    }

    public void Insert(IEnumerable<AiProvenanceDto> rows)
    {
        foreach (AiProvenanceDto row in rows)
        {
            row.Id = 0;
            Database.Insert(row);
        }
    }

    public void Delete(IEnumerable<int> ids)
    {
        int[] idArray = ids.ToArray();
        if (idArray.Length == 0)
        {
            return;
        }

        Database.Execute(Database.SqlContext.Sql()
            .Delete<AiProvenanceDto>()
            .WhereIn<AiProvenanceDto>(x => x.Id, idArray));
    }

    public void DeleteForDocument(Guid documentKey) =>
        Database.Execute(Database.SqlContext.Sql()
            .Delete<AiProvenanceDto>()
            .Where<AiProvenanceDto>(x => x.DocumentKey == documentKey));
}

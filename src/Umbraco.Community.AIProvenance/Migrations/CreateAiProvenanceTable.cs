using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Community.AIProvenance.Persistence;

namespace Umbraco.Community.AIProvenance.Migrations;

/// <summary>
/// Creates the <c>aiProvenance</c> table.
/// </summary>
public sealed class CreateAiProvenanceTable : AsyncMigrationBase
{
    public CreateAiProvenanceTable(IMigrationContext context)
        : base(context)
    {
    }

    protected override Task MigrateAsync()
    {
        if (TableExists(AiProvenanceDto.TableName) is false)
        {
            Create.Table<AiProvenanceDto>().Do();
        }

        return Task.CompletedTask;
    }
}

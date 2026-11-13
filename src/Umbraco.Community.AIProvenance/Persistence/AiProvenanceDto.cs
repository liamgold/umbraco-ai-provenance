using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace Umbraco.Community.AIProvenance.Persistence;

/// <summary>
/// One row of the <c>aiProvenance</c> table.
/// </summary>
[TableName(TableName)]
[PrimaryKey("id", AutoIncrement = true)]
[ExplicitColumns]
internal sealed class AiProvenanceDto
{
    public const string TableName = "aiProvenance";

    [Column("id")]
    [PrimaryKeyColumn(AutoIncrement = true)]
    public int Id { get; set; }

    // SQL Server treats nulls as equal in a unique index, so this enforces one row per property value.
    // SQLite does not, so the repository also replaces rows rather than relying on the index.
    [Column("documentKey")]
    [Index(IndexTypes.UniqueNonClustered, Name = "IX_" + TableName + "_Property",
        ForColumns = "documentKey,elementKey,propertyAlias,culture,segment,state")]
    public Guid DocumentKey { get; set; }

    [Column("elementKey")]
    [NullSetting(NullSetting = NullSettings.Null)]
    public Guid? ElementKey { get; set; }

    [Column("propertyAlias")]
    [Length(255)]
    public string PropertyAlias { get; set; } = string.Empty;

    [Column("culture")]
    [Length(50)]
    [NullSetting(NullSetting = NullSettings.Null)]
    public string? Culture { get; set; }

    [Column("segment")]
    [Length(255)]
    [NullSetting(NullSetting = NullSettings.Null)]
    public string? Segment { get; set; }

    [Column("state")]
    public byte State { get; set; }

    [Column("origin")]
    public byte Origin { get; set; }

    [Column("source")]
    public byte Source { get; set; }

    [Column("aiProvider")]
    [Length(100)]
    [NullSetting(NullSetting = NullSettings.Null)]
    public string? AiProvider { get; set; }

    [Column("aiModel")]
    [Length(255)]
    [NullSetting(NullSetting = NullSettings.Null)]
    public string? AiModel { get; set; }

    [Column("updatedBy")]
    public Guid UpdatedBy { get; set; }

    [Column("updatedUtc")]
    public DateTime UpdatedUtc { get; set; }
}

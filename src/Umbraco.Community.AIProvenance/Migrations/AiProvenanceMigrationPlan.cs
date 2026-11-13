using Umbraco.Cms.Core.Packaging;

namespace Umbraco.Community.AIProvenance.Migrations;

/// <summary>
/// Creates and updates the package's database table. Umbraco finds and runs this plan at startup.
/// </summary>
public sealed class AiProvenanceMigrationPlan : PackageMigrationPlan
{
    public AiProvenanceMigrationPlan()
        : base(Constants.PackageId, Constants.PackageId, Constants.PackageId)
    {
    }

    protected override void DefinePlan()
    {
        // Never change or remove a step that has shipped. Add new steps at the end.
        To<CreateAiProvenanceTable>("{5B2E0B0B-6C1E-4C55-9C43-7A1F3C2B9D01}");
    }
}

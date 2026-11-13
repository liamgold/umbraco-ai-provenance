using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Install;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Community.AIProvenance.Composers;
using Umbraco.Community.AIProvenance.Migrations;
using Umbraco.Community.AIProvenance.Services;

namespace Umbraco.Community.AIProvenance.Tests;

/// <summary>
/// Starts Umbraco with the package registered and its table created.
/// </summary>
public abstract class AiProvenanceTestBase : UmbracoIntegrationTestWithContent
{
    protected IAiProvenanceService ProvenanceService => GetRequiredService<IAiProvenanceService>();

    protected static Guid UserKey => Umbraco.Cms.Core.Constants.Security.SuperUserKey;

    protected override void SetUpTestConfiguration(IConfigurationBuilder configBuilder)
    {
        base.SetUpTestConfiguration(configBuilder);
        configBuilder.AddJsonFile(Path.Combine(TestContext.CurrentContext.TestDirectory, "appsettings.Tests.json"));

        // Environment variables override the file, so a run can switch database:
        // Tests__Database__DatabaseType=LocalDb runs the tests against SQL Server LocalDB.
        configBuilder.AddEnvironmentVariables();
    }

    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        // Umbraco's test host does not run composers, so register the package's services here.
        new AiProvenanceComposer().Compose(builder);
        builder.PackageMigrationPlans().Add<AiProvenanceMigrationPlan>();
    }

    // appsettings.Tests.json turns off unattended package migrations. Otherwise Umbraco starts this plan on a
    // background service at boot, it races this method to create the same table in the in-memory SQLite database,
    // and the two connections lock each other until the test run times out.
    [SetUp]
    public async Task CreateTable() =>
        await GetRequiredService<PackageMigrationRunner>().RunPackagePlansAsync([Constants.PackageId]);
}

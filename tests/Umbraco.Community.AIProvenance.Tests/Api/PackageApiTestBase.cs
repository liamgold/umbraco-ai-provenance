using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Umbraco.Cms.Api.Management.Controllers.Document;
using Umbraco.Cms.Infrastructure.Install;
using Umbraco.Cms.Tests.Integration.ManagementApi;

namespace Umbraco.Community.AIProvenance.Tests.Api;

/// <summary>
/// Starts Umbraco's HTTP test server with the package's API and table, for tests that call the package's endpoints.
/// </summary>
/// <remarks>
/// <see cref="ManagementApiTest{T}"/> provides the test server and signs users in. Its controller type only
/// matters for URL helpers, which these tests do not use: they call the package's routes directly.
/// </remarks>
public abstract class PackageApiTestBase : ManagementApiTest<ByKeyDocumentController>
{
    protected const string ApiRoot = "/umbraco/umbracocommunityaiprovenance/api/v1";

    protected override Expression<Func<ByKeyDocumentController, object>> MethodSelector { get; set; } =
        x => x.ByKey(CancellationToken.None, Guid.Empty);

    // MVC finds controllers in the app's entry assembly and its references. The test server's entry assembly is
    // Umbraco's test assembly, which does not reference the package, so add the package's controllers.
    protected override void CustomMvcSetup(IMvcBuilder mvcBuilder) =>
        mvcBuilder.AddApplicationPart(typeof(Umbraco.Community.AIProvenance.Api.DocumentMarksController).Assembly);

    [SetUp]
    public async Task CreateTable() =>
        await GetRequiredService<PackageMigrationRunner>().RunPackagePlansAsync([Constants.PackageId]);
}

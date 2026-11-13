using NUnit.Framework;

// No namespace, so NUnit runs this once for the whole test assembly.

/// <summary>
/// Runs Umbraco's own test assembly setup for this assembly.
/// </summary>
/// <remarks>
/// Umbraco's <see cref="GlobalSetupTeardown"/> loads appsettings.Tests.json (and environment variables) into
/// <see cref="GlobalSetupTeardown.TestConfiguration"/>, which Umbraco's test server reads. NUnit only runs a
/// setup fixture in its own assembly, so this fixture calls it. Its teardown removes the test databases.
/// </remarks>
[SetUpFixture]
public sealed class TestAssemblySetup
{
    private readonly GlobalSetupTeardown _umbracoSetup = new();

    [OneTimeSetUp]
    public void SetUp()
    {
        // Umbraco's HTTP test server (WebApplicationFactory) looks for its content root next to a solution file,
        // which only works in Umbraco's own repository. This variable points it at the test output folder instead.
        Environment.SetEnvironmentVariable("ASPNETCORE_TEST_CONTENTROOT_UMBRACO_TESTS_INTEGRATION", AppContext.BaseDirectory);

        _umbracoSetup.SetUp();
    }

    [OneTimeTearDown]
    public void TearDown() => _umbracoSetup.TearDown();
}

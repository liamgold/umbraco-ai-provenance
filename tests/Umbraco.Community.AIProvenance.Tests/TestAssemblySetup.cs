using NUnit.Framework;
using Umbraco.Cms.Tests.Integration.Testing;

// No namespace, so NUnit runs this once for the whole test assembly.

/// <summary>
/// Removes the test databases after all tests have run.
/// </summary>
[SetUpFixture]
public sealed class TestAssemblySetup
{
    [OneTimeTearDown]
    public void TearDown() => BaseTestDatabase.Instance?.TearDown();
}

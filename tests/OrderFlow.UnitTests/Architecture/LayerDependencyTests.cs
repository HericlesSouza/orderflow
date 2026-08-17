using System.Xml.Linq;

namespace OrderFlow.UnitTests.Architecture;

[Trait("Layer", "Architecture")]
[Trait("Feature", "Common")]
public sealed class LayerDependencyTests
{
    public static IEnumerable<TheoryDataRow<string, string[]>> ExpectedProjectReferences()
    {
        yield return new TheoryDataRow<string, string[]>("OrderFlow.Domain", [])
        {
            TestDisplayName = "Domain has no project dependencies"
        };

        yield return new TheoryDataRow<string, string[]>("OrderFlow.Application", ["OrderFlow.Domain"])
        {
            TestDisplayName = "Application depends only on Domain"
        };

        yield return new TheoryDataRow<string, string[]>(
            "OrderFlow.Infrastructure",
            ["OrderFlow.Application", "OrderFlow.Domain"])
        {
            TestDisplayName = "Infrastructure depends on Application and Domain"
        };

        yield return new TheoryDataRow<string, string[]>(
            "OrderFlow.Api",
            ["OrderFlow.Application", "OrderFlow.Infrastructure"])
        {
            TestDisplayName = "Api depends on Application and Infrastructure"
        };
    }

    [Theory]
    [MemberData(nameof(ExpectedProjectReferences))]
    public void Should_Have_Expected_Project_References_When_Architecture_Is_Validated(
        string projectName, string[] expectedReferences)
    {
        var repositoryRoot = FindRepositoryRoot();
        var projectPath = Path.Combine(repositoryRoot, "src", projectName, $"{projectName}.csproj");
        var project = XDocument.Load(projectPath);

        var actualReferences = project
            .Descendants("ProjectReference")
            .Select(reference => Path.GetFileNameWithoutExtension(reference.Attribute("Include")!.Value))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedReferences.Order(StringComparer.Ordinal), actualReferences);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OrderFlow.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not find the repository root.");
    }
}

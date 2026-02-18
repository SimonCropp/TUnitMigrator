namespace testing;

public class YmlMigratorTests
{
    [Test]
    public async Task DotnetTestWithDirectory()
    {
        var input = """
                    name: CI
                    on: push
                    jobs:
                      build:
                        runs-on: ubuntu-latest
                        steps:
                          - uses: actions/checkout@v4
                          - run: dotnet test src --configuration Release
                    """;

        // Create a temp directory with a solution file
        var tempDir = Path.Combine(Path.GetTempPath(), "TUnitMigratorYmlTests", Guid.NewGuid().ToString());
        var srcDir = Path.Combine(tempDir, "src");
        Directory.CreateDirectory(srcDir);
        File.WriteAllText(Path.Combine(srcDir, "MySolution.slnx"), "<Solution />");

        try
        {
            var result = YmlMigrator.MigrateContent(input, tempDir);
            await Assert.That(result).Contains("dotnet test --solution src/MySolution.slnx --configuration Release");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task DotnetTestWithFlagOnly()
    {
        var input = "      - run: dotnet test --configuration Release";

        var tempDir = Path.Combine(Path.GetTempPath(), "TUnitMigratorYmlTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var result = YmlMigrator.MigrateContent(input, tempDir);
            // Should not be modified since --configuration is a flag, not a directory
            await Assert.That(result).IsEqualTo(input);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task PreservesYamlStructure()
    {
        var input = """
                    name: CI
                    on: push
                    jobs:
                      build:
                        steps:
                          - run: dotnet build
                          - run: dotnet test src --configuration Release --no-build
                    """;

        var tempDir = Path.Combine(Path.GetTempPath(), "TUnitMigratorYmlTests", Guid.NewGuid().ToString());
        var srcDir = Path.Combine(tempDir, "src");
        Directory.CreateDirectory(srcDir);
        File.WriteAllText(Path.Combine(srcDir, "Test.sln"), "");

        try
        {
            var result = YmlMigrator.MigrateContent(input, tempDir);
            await Assert.That(result).Contains("dotnet test --solution src/Test.sln --configuration Release --no-build");
            await Assert.That(result).Contains("dotnet build");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}

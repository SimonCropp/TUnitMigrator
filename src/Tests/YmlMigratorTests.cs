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

        using var tempDir = new TempDirectory();
        var srcDir = Path.Combine(tempDir, "src");
        Directory.CreateDirectory(srcDir);
        await File.WriteAllTextAsync(Path.Combine(srcDir, "MySolution.slnx"), "<Solution />");

        var result = YmlMigrator.MigrateContent(input, tempDir);
        await Assert.That(result).Contains("dotnet test --solution src/MySolution.slnx --configuration Release");
    }

    [Test]
    public async Task DotnetTestWithFlagOnly()
    {
        var input = "      - run: dotnet test --configuration Release";
        using var tempDir = new TempDirectory();

        var result = YmlMigrator.MigrateContent(input, tempDir);
        // Should not be modified since --configuration is a flag, not a directory
        await Assert.That(result).IsEqualTo(input);
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

        using var tempDir = new TempDirectory();
        var srcDir = Path.Combine(tempDir, "src");
        Directory.CreateDirectory(srcDir);
        await File.WriteAllTextAsync(Path.Combine(srcDir, "Test.sln"), "");

        var result = YmlMigrator.MigrateContent(input, tempDir);
        await Assert.That(result).Contains("dotnet test --solution src/Test.sln --configuration Release --no-build");
        await Assert.That(result).Contains("dotnet build");
    }

    [Test]
    public async Task PrefersSlnxOverSln()
    {
        var input = "      - run: dotnet test src";

        using var tempDir = new TempDirectory();
        var srcDir = Path.Combine(tempDir, "src");
        Directory.CreateDirectory(srcDir);
        await File.WriteAllTextAsync(Path.Combine(srcDir, "Old.sln"), "");
        await File.WriteAllTextAsync(Path.Combine(srcDir, "New.slnx"), "<Solution />");

        var result = YmlMigrator.MigrateContent(input, tempDir);
        await Assert.That(result).Contains("dotnet test --solution src/New.slnx");
    }

    [Test]
    public async Task SkipsWhenNoSolutionFile()
    {
        var input = "      - run: dotnet test src";

        using var tempDir = new TempDirectory();
        Directory.CreateDirectory(Path.Combine(tempDir, "src"));

        var result = YmlMigrator.MigrateContent(input, tempDir);
        await Assert.That(result).IsEqualTo(input);
    }

    [Test]
    public async Task SkipsWhenDirectoryDoesNotExist()
    {
        var input = "      - run: dotnet test nonexistent";

        using var tempDir = new TempDirectory();

        var result = YmlMigrator.MigrateContent(input, tempDir);
        await Assert.That(result).IsEqualTo(input);
    }

    [Test]
    public async Task HandlesMultipleDotnetTestLines()
    {
        var input = """
                          - run: dotnet test src
                          - run: dotnet test tests
                    """;

        using var tempDir = new TempDirectory();
        var srcDir = Path.Combine(tempDir, "src");
        var testsDir = Path.Combine(tempDir, "tests");
        Directory.CreateDirectory(srcDir);
        Directory.CreateDirectory(testsDir);
        await File.WriteAllTextAsync(Path.Combine(srcDir, "Src.slnx"), "<Solution />");
        await File.WriteAllTextAsync(Path.Combine(testsDir, "Tests.sln"), "");

        var result = YmlMigrator.MigrateContent(input, tempDir);
        await Assert.That(result).Contains("dotnet test --solution src/Src.slnx");
        await Assert.That(result).Contains("dotnet test --solution tests/Tests.sln");
    }

    [Test]
    public async Task HandlesGitHubActionsRunPrefix()
    {
        var input = "        - run: dotnet test src";

        using var tempDir = new TempDirectory();
        var srcDir = Path.Combine(tempDir, "src");
        Directory.CreateDirectory(srcDir);
        await File.WriteAllTextAsync(Path.Combine(srcDir, "App.slnx"), "<Solution />");

        var result = YmlMigrator.MigrateContent(input, tempDir);
        await Assert.That(result).Contains("dotnet test --solution src/App.slnx");
    }

    [Test]
    public async Task MigrateWritesFileWhenChanged()
    {
        using var tempDir = new TempDirectory();
        var srcDir = Path.Combine(tempDir, "src");
        Directory.CreateDirectory(srcDir);
        await File.WriteAllTextAsync(Path.Combine(srcDir, "App.slnx"), "<Solution />");

        var ymlPath = Path.Combine(tempDir, "ci.yml");
        await File.WriteAllTextAsync(ymlPath, "      - run: dotnet test src");

        await YmlMigrator.Migrate(tempDir);

        var result = await File.ReadAllTextAsync(ymlPath);
        await Assert.That(result).Contains("dotnet test --solution src/App.slnx");
    }

    [Test]
    public async Task MigrateDoesNotWriteWhenUnchanged()
    {
        using var tempDir = new TempDirectory();
        var ymlPath = Path.Combine(tempDir, "ci.yml");
        var content = "      - run: dotnet build";
        await File.WriteAllTextAsync(ymlPath, content);
        var lastWrite = File.GetLastWriteTimeUtc(ymlPath);

        // Small delay to detect write time changes
        await Task.Delay(50);
        await YmlMigrator.Migrate(tempDir);

        await Assert.That(File.GetLastWriteTimeUtc(ymlPath)).IsEqualTo(lastWrite);
    }
}

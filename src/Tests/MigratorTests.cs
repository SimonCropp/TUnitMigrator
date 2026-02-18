namespace testing;

public class MigratorTests
{
    static string ScenariosDir =>
        Path.Combine(
            Path.GetDirectoryName(typeof(MigratorTests).Assembly.Location)!,
            "..", "..", "..", "..", "Scenarios");

    static string CopyScenarioToTemp(string scenarioName)
    {
        var source = Path.GetFullPath(Path.Combine(ScenariosDir, scenarioName));
        var tempDir = Path.Combine(Path.GetTempPath(), "TUnitMigratorTests", Guid.NewGuid().ToString(), scenarioName);
        CopyDirectory(source, tempDir);
        return tempDir;
    }

    static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var file in Directory.EnumerateFiles(source))
        {
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
        }

        foreach (var dir in Directory.EnumerateDirectories(source))
        {
            var dirName = Path.GetFileName(dir);
            CopyDirectory(dir, Path.Combine(destination, dirName));
        }
    }

    [Test]
    public async Task MSTestMigration()
    {
        var tempDir = CopyScenarioToTemp("MSTestScenario");
        try
        {
            await Migrator.Migrate(tempDir);

            var props = await File.ReadAllTextAsync(Path.Combine(tempDir, "Directory.Packages.props"));
            var csproj = await File.ReadAllTextAsync(Path.Combine(tempDir, "src", "TestProject.csproj"));
            var yml = await File.ReadAllTextAsync(Path.Combine(tempDir, ".github", "workflows", "ci.yml"));
            var globalJson = await File.ReadAllTextAsync(Path.Combine(tempDir, "global.json"));

            await Verify(
                new
                {
                    props,
                    csproj,
                    yml,
                    globalJson
                });
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task NUnitMigration()
    {
        var tempDir = CopyScenarioToTemp("NUnitScenario");
        try
        {
            await Migrator.Migrate(tempDir);

            var props = await File.ReadAllTextAsync(Path.Combine(tempDir, "Directory.Packages.props"));
            var csproj = await File.ReadAllTextAsync(Path.Combine(tempDir, "src", "TestProject.csproj"));
            var yml = await File.ReadAllTextAsync(Path.Combine(tempDir, ".github", "workflows", "ci.yml"));
            var globalJson = await File.ReadAllTextAsync(Path.Combine(tempDir, "global.json"));

            await Verify(
                new
                {
                    props,
                    csproj,
                    yml,
                    globalJson
                });
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task XunitMigration()
    {
        var tempDir = CopyScenarioToTemp("XunitScenario");
        try
        {
            await Migrator.Migrate(tempDir);

            var props = await File.ReadAllTextAsync(Path.Combine(tempDir, "Directory.Packages.props"));
            var csproj = await File.ReadAllTextAsync(Path.Combine(tempDir, "src", "TestProject.csproj"));
            var yml = await File.ReadAllTextAsync(Path.Combine(tempDir, ".github", "workflows", "ci.yml"));
            var globalJson = await File.ReadAllTextAsync(Path.Combine(tempDir, "global.json"));

            await Verify(
                new
                {
                    props,
                    csproj,
                    yml,
                    globalJson
                });
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task XunitV3Migration()
    {
        var tempDir = CopyScenarioToTemp("XunitV3Scenario");
        try
        {
            await Migrator.Migrate(tempDir);

            var props = await File.ReadAllTextAsync(Path.Combine(tempDir, "Directory.Packages.props"));
            var csproj = await File.ReadAllTextAsync(Path.Combine(tempDir, "src", "TestProject.csproj"));
            var yml = await File.ReadAllTextAsync(Path.Combine(tempDir, ".github", "workflows", "ci.yml"));
            var globalJson = await File.ReadAllTextAsync(Path.Combine(tempDir, "global.json"));

            await Verify(
                new
                {
                    props,
                    csproj,
                    yml,
                    globalJson
                });
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    static Task Verify(object target) =>
        VerifyTUnit.Verifier.Verify(target);
}

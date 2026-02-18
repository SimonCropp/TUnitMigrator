public class MigratorTests
{
    static string ScenariosDir => Path.Combine(ProjectFiles.SolutionDirectory, "Scenarios");

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
        var targetDir = CopyScenarioToTemp("MSTestScenario");
        try
        {
            await Migrator.Migrate(targetDir);

            var props = await File.ReadAllTextAsync(Path.Combine(targetDir, "Directory.Packages.props"));
            var csproj = await File.ReadAllTextAsync(Path.Combine(targetDir, "src", "TestProject.csproj"));
            var yml = await File.ReadAllTextAsync(Path.Combine(targetDir, ".github", "workflows", "ci.yml"));
            var globalJson = await File.ReadAllTextAsync(Path.Combine(targetDir, "global.json"));

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
            Directory.Delete(targetDir, true);
        }
    }

    [Test]
    public async Task NUnitMigration()
    {
        var targetDir = CopyScenarioToTemp("NUnitScenario");
        try
        {
            await Migrator.Migrate(targetDir);

            var props = await File.ReadAllTextAsync(Path.Combine(targetDir, "Directory.Packages.props"));
            var csproj = await File.ReadAllTextAsync(Path.Combine(targetDir, "src", "TestProject.csproj"));
            var yml = await File.ReadAllTextAsync(Path.Combine(targetDir, ".github", "workflows", "ci.yml"));
            var globalJson = await File.ReadAllTextAsync(Path.Combine(targetDir, "global.json"));

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
            Directory.Delete(targetDir, true);
        }
    }

    [Test]
    public async Task XunitMigration()
    {
        var targetDir = CopyScenarioToTemp("XunitScenario");
        try
        {
            await Migrator.Migrate(targetDir);

            var props = await File.ReadAllTextAsync(Path.Combine(targetDir, "Directory.Packages.props"));
            var csproj = await File.ReadAllTextAsync(Path.Combine(targetDir, "src", "TestProject.csproj"));
            var yml = await File.ReadAllTextAsync(Path.Combine(targetDir, ".github", "workflows", "ci.yml"));
            var globalJson = await File.ReadAllTextAsync(Path.Combine(targetDir, "global.json"));

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
            Directory.Delete(targetDir, true);
        }
    }

    [Test]
    public async Task XunitV3Migration()
    {
        var targetDir = CopyScenarioToTemp("XunitV3Scenario");
        try
        {
            await Migrator.Migrate(targetDir);

            var props = await File.ReadAllTextAsync(Path.Combine(targetDir, "Directory.Packages.props"));
            var csproj = await File.ReadAllTextAsync(Path.Combine(targetDir, "src", "TestProject.csproj"));
            var yml = await File.ReadAllTextAsync(Path.Combine(targetDir, ".github", "workflows", "ci.yml"));
            var globalJson = await File.ReadAllTextAsync(Path.Combine(targetDir, "global.json"));

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
            Directory.Delete(targetDir, true);
        }
    }

    static Task Verify(object target) =>
        VerifyTUnit.Verifier.Verify(target);
}

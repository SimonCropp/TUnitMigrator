static class Migrator
{
    public static async Task Migrate(string directory)
    {
        if (!Directory.Exists(directory))
        {
            Log.Error("Directory does not exist: {Directory}", directory);
            return;
        }

        var projectRoots = FileSystem.FindProjectRoots(directory);

        if (projectRoots.Count == 0)
        {
            Log.Warning("No project roots found (no .git directories) in {Directory}", directory);
            return;
        }

        Log.Information("Found {Count} project root(s) to migrate", projectRoots.Count);

        using var cache = new SourceCacheContext
        {
            RefreshMemoryCache = true
        };

        foreach (var root in projectRoots)
        {
            await MigrateProjectRoot(root, cache);
        }
    }

    static async Task MigrateProjectRoot(string projectRoot, SourceCacheContext cache)
    {
        Log.Information("Migrating project root: {Root}", projectRoot);

        // Find Directory.Packages.props
        var propsFiles = FileSystem.EnumerateFiles(projectRoot, "Directory.Packages.props").ToList();

        if (propsFiles.Count == 0)
        {
            Log.Error("No Directory.Packages.props found in {Root}. Central Package Management is required.", projectRoot);
            return;
        }

        var propsPath = propsFiles[0];
        var propsXml = XDocument.Load(propsPath);

        // Validate CPM is enabled
        var cpmEnabled = propsXml.Descendants("ManagePackageVersionsCentrally")
            .FirstOrDefault()?.Value;

        if (!string.Equals(cpmEnabled, "true", StringComparison.OrdinalIgnoreCase))
        {
            Log.Error("Central Package Management is not enabled in {Props}", propsPath);
            return;
        }

        // Detect test framework
        var framework = FrameworkDetector.Detect(propsXml);
        if (framework == TestFramework.None)
        {
            Log.Warning("No test framework detected in {Props}", propsPath);
            return;
        }

        Log.Information("Detected test framework: {Framework}", framework);

        // Query NuGet for latest TUnit version
        var sources = PackageSourceReader.Read(projectRoot);
        var tunitVersion = await NuGetPackageChecker.GetLatestStableVersion("TUnit", sources, cache);

        if (tunitVersion == null)
        {
            Log.Error("Could not find TUnit package on NuGet");
            return;
        }

        Log.Information("Latest TUnit version: {Version}", tunitVersion);

        // Add TUnit to props and csprojs first (old framework still present so code compiles)
        await TUnitAdder.AddToProps(propsPath, tunitVersion);
        await TUnitAdder.AddToCsprojs(projectRoot);

        // Run CodeMigrator (dotnet format analyzers) while both old and new frameworks are present
        await CodeMigrator.Migrate(projectRoot);

        // Run PackagesMigrator (removes old framework packages, handles extensions)
        var migrations = await PackagesMigrator.Migrate(propsPath, tunitVersion, sources, cache);

        // Run CsprojMigrator (removes old PackageReferences from csprojs)
        await CsprojMigrator.Migrate(projectRoot, migrations);

        // Run YmlMigrator
        await YmlMigrator.Migrate(projectRoot);

        // Run GlobalJsonRelocator
        await GlobalJsonRelocator.Relocate(projectRoot);

        Log.Information("Migration complete for {Root}", projectRoot);
    }
}

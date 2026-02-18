static class YmlMigrator
{
    static readonly Regex dotnetTestRegex = new(
        @"^(\s*(?:-\s*(?:run:\s*)?)?)(dotnet\s+test)\s+(\S+)(.*)",
        RegexOptions.Compiled);

    public static async Task Migrate(string directory)
    {
        var ymlFiles = FileSystem.EnumerateFiles(directory, "*.yml");

        foreach (var ymlPath in ymlFiles)
        {
            var content = await File.ReadAllTextAsync(ymlPath);
            var newContent = MigrateContent(content, directory);

            if (content != newContent)
            {
                await File.WriteAllTextAsync(ymlPath, newContent);
                Log.Information("Updated YML file: {File}", ymlPath);
            }
        }
    }

    internal static string MigrateContent(string content, string rootDirectory)
    {
        var lines = content.Split('\n');
        var updated = false;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var match = dotnetTestRegex.Match(line);

            if (!match.Success)
            {
                continue;
            }

            var prefix = match.Groups[1].Value;
            var command = match.Groups[2].Value;
            var dirArg = match.Groups[3].Value;
            var rest = match.Groups[4].Value;

            // Skip if the argument starts with -- (it's a flag, not a directory)
            if (dirArg.StartsWith("--"))
            {
                continue;
            }

            // Find solution file in the referenced directory
            var resolvedDir = Path.Combine(rootDirectory, dirArg.Replace('/', Path.DirectorySeparatorChar));
            string? solutionFile = null;

            if (Directory.Exists(resolvedDir))
            {
                solutionFile = FileSystem.FindSolutionFile(resolvedDir);
            }

            if (solutionFile == null)
            {
                Log.Warning("No solution file found in {Directory} for YML migration", resolvedDir);
                continue;
            }

            var solutionName = Path.GetFileName(solutionFile);
            var solutionRelative = $"{dirArg}/{solutionName}";

            lines[i] = $"{prefix}dotnet test --solution {solutionRelative}{rest}";
            updated = true;
            Log.Information("Migrated dotnet test command in YML: {Old} -> {New}", line.Trim(), lines[i].Trim());
        }

        return updated ? string.Join('\n', lines) : content;
    }
}

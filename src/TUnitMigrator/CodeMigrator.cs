static class CodeMigrator
{
    static readonly Dictionary<TestFramework, string> diagnosticIds = new()
    {
        [TestFramework.MSTest] = "TUMS0001",
        [TestFramework.NUnit] = "TUNU0001",
        [TestFramework.Xunit] = "TUXU0001",
        [TestFramework.XunitV3] = "TUXU0001"
    };

    public static async Task Migrate(string projectRoot, TestFramework framework)
    {
        if (!diagnosticIds.TryGetValue(framework, out var diagnosticId))
        {
            Log.Warning("No code migration diagnostic for framework {Framework}", framework);
            return;
        }

        var solutionFile = FindSolutionFileRecursive(projectRoot);

        if (solutionFile == null)
        {
            Log.Warning("No solution file found in {Root}, skipping code migration", projectRoot);
            return;
        }

        Log.Information("Running dotnet format analyzers with {DiagnosticId} on {Solution}", diagnosticId, solutionFile);

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"format analyzers \"{solutionFile}\" --severity info --diagnostics {diagnosticId}",
                WorkingDirectory = projectRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        process.Start();
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (!string.IsNullOrWhiteSpace(stdout))
        {
            Log.Information("dotnet format output: {Output}", stdout.TrimEnd());
        }

        if (process.ExitCode != 0)
        {
            Log.Error("dotnet format exited with code {ExitCode}: {Error}", process.ExitCode, stderr.TrimEnd());
        }
        else
        {
            Log.Information("Code migration complete for {Solution}", solutionFile);
        }
    }

    static string? FindSolutionFileRecursive(string directory)
    {
        // Check each directory level for a solution file, starting from root
        var solution = FileSystem.FindSolutionFile(directory);
        if (solution != null)
        {
            return solution;
        }

        // Search subdirectories
        foreach (var subdir in Directory.EnumerateDirectories(directory))
        {
            solution = FileSystem.FindSolutionFile(subdir);
            if (solution != null)
            {
                return solution;
            }
        }

        return null;
    }
}

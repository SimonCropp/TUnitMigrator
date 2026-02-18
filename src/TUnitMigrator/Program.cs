Logging.Init();
await CommandRunner.RunCommand(Inner, args);

static async Task Inner(string directory)
{
    Log.Information("TargetDirectory: {TargetDirectory}", directory);

    if (!Directory.Exists(directory))
    {
        Log.Information("Target directory does not exist: {TargetDirectory}", directory);
        Environment.Exit(1);
    }

    var totalStopwatch = Stopwatch.StartNew();
    await Migrator.Migrate(directory);
    Log.Information("Completed in {Elapsed}", Formatter.FormatElapsed(totalStopwatch.Elapsed));
}

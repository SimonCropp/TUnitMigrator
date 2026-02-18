static partial class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init() =>
        VerifierSettings.AddScrubber(ScrubPackageVersions);

    static void ScrubPackageVersions(StringBuilder builder)
    {
        var content = builder.ToString();
        var scrubbed = VersionRegex().Replace(content, """<PackageVersion Include="$1" Version="{$1.Version}" />""");

        if (content != scrubbed)
        {
            builder.Clear();
            builder.Append(scrubbed);
        }
    }

    [GeneratedRegex("""<PackageVersion Include="([^"]+)" Version="[^"]+" />""")]
    private static partial Regex VersionRegex();
}

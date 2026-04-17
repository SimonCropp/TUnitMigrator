static partial class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifierSettings.AddScrubber(ScrubPackageVersions);
        VerifierSettings.AddScrubber(ScrubSdkVersion);
    }

    static void ScrubPackageVersions(StringBuilder builder) =>
        ReplaceInPlace(builder, VersionRegex(), """<PackageVersion Include="$1" Version="{$1.Version}" />""");

    static void ScrubSdkVersion(StringBuilder builder) =>
        ReplaceInPlace(builder, SdkVersionRegex(), """version": "{SdkVersion}""");

    static void ReplaceInPlace(StringBuilder builder, Regex regex, string replacement)
    {
        var content = builder.ToString();
        var scrubbed = regex.Replace(content, replacement);

        if (content != scrubbed)
        {
            builder.Clear();
            builder.Append(scrubbed);
        }
    }

    [GeneratedRegex("""<PackageVersion Include="([^"]+)" Version="[^"]+" />""")]
    private static partial Regex VersionRegex();

    [GeneratedRegex("""version": "[^"]+""")]
    private static partial Regex SdkVersionRegex();
}

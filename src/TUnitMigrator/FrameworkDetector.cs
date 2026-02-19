static class FrameworkDetector
{
    static readonly List<string> testFrameworkPrefixes = ["MSTest", "NUnit", "xunit"];

    public static bool HasTestFramework(XDocument propsXml) =>
        propsXml
            .Descendants("PackageVersion")
            .Select(_ => _.Attribute("Include")?.Value)
            .Where(_ => _ != null)
            .Any(name => testFrameworkPrefixes.Any(prefix => name!.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)));
}

enum TestFramework
{
    None,
    MSTest,
    NUnit,
    Xunit,
    XunitV3
}

static class FrameworkDetector
{
    public static HashSet<string> GetPackageNames(TestFramework framework) =>
        framework switch
        {
            TestFramework.MSTest => new(StringComparer.OrdinalIgnoreCase) { "MSTest", "MSTest.TestFramework", "MSTest.TestAdapter", "MSTest.Analyzers" },
            TestFramework.NUnit => new(StringComparer.OrdinalIgnoreCase) { "NUnit", "NUnit3TestAdapter", "NUnit.ConsoleRunner", "NUnit.Analyzers", "NUnit.Console" },
            TestFramework.Xunit => new(StringComparer.OrdinalIgnoreCase) { "xunit", "xunit.runner.visualstudio", "xunit.abstractions" },
            TestFramework.XunitV3 => new(StringComparer.OrdinalIgnoreCase) { "xunit.v3", "xunit.runner.visualstudio", "xunit.abstractions" },
            _ => []
        };

    public static List<string> GetPackagePrefixesToRemove(TestFramework framework) =>
        framework switch
        {
            TestFramework.MSTest => ["Microsoft.Testing."],
            TestFramework.Xunit => ["xunit.extensibility."],
            TestFramework.XunitV3 => ["xunit.extensibility."],
            _ => []
        };

    public static TestFramework Detect(XDocument propsXml)
    {
        var packageNames = propsXml
            .Descendants("PackageVersion")
            .Select(_ => _.Attribute("Include")?.Value)
            .Where(_ => _ != null)
            .ToList();

        // Check xunit.v3 first (more specific)
        if (packageNames.Any(_ => string.Equals(_, "xunit.v3", StringComparison.OrdinalIgnoreCase)))
        {
            return TestFramework.XunitV3;
        }

        if (packageNames.Any(_ => string.Equals(_, "xunit", StringComparison.OrdinalIgnoreCase)))
        {
            return TestFramework.Xunit;
        }

        if (packageNames.Any(_ => string.Equals(_, "NUnit", StringComparison.OrdinalIgnoreCase)))
        {
            return TestFramework.NUnit;
        }

        if (packageNames.Any(_ =>
                string.Equals(_, "MSTest", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(_, "MSTest.TestFramework", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(_, "MSTest.TestAdapter", StringComparison.OrdinalIgnoreCase)))
        {
            return TestFramework.MSTest;
        }

        return TestFramework.None;
    }
}

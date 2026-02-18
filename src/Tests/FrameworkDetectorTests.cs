public class FrameworkDetectorTests
{
    static XDocument Props(params string[] packageNames)
    {
        var itemGroup = new XElement("ItemGroup");
        foreach (var name in packageNames)
        {
            itemGroup.Add(new XElement("PackageVersion",
                new XAttribute("Include", name),
                new XAttribute("Version", "1.0.0")));
        }

        return new(new XElement("Project", itemGroup));
    }

    [Test]
    [Arguments("MSTest", "MSTest")]
    [Arguments("MSTest.TestFramework", "MSTest")]
    [Arguments("MSTest.TestAdapter", "MSTest")]
    [Arguments("MSTEST", "MSTest")]
    [Arguments("NUnit", "NUnit")]
    [Arguments("xunit", "Xunit")]
    [Arguments("xunit.v3", "XunitV3")]
    [Arguments("Newtonsoft.Json", "None")]
    public async Task Detect(string packageName, string expected) =>
        await Assert.That(FrameworkDetector.Detect(Props(packageName))).IsEqualTo(Enum.Parse<TestFramework>(expected));

    [Test]
    public async Task XunitV3TakesPriorityOverXunit() =>
        await Assert.That(FrameworkDetector.Detect(Props("xunit", "xunit.v3"))).IsEqualTo(TestFramework.XunitV3);
}

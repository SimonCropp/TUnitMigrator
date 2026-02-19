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
    [Arguments("MSTest")]
    [Arguments("MSTest.TestFramework")]
    [Arguments("MSTest.TestAdapter")]
    [Arguments("MSTEST")]
    [Arguments("NUnit")]
    [Arguments("xunit")]
    [Arguments("xunit.v3")]
    public async Task DetectsTestFramework(string packageName) =>
        await Assert.That(FrameworkDetector.HasTestFramework(Props(packageName))).IsTrue();

    [Test]
    [Arguments("Newtonsoft.Json")]
    [Arguments("Serilog")]
    public async Task NoTestFrameworkDetected(string packageName) =>
        await Assert.That(FrameworkDetector.HasTestFramework(Props(packageName))).IsFalse();
}

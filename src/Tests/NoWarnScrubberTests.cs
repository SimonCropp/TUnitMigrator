public class NoWarnScrubberTests
{
    [Test]
    public async Task RemovesXunitWarningsFromNoWarn()
    {
        var xml = XDocument.Parse(
            """
            <Project>
              <PropertyGroup>
                <NoWarn>$(NoWarn);CS0649;CS8618;CS0105;xUnit1013;xUnit1051</NoWarn>
              </PropertyGroup>
            </Project>
            """);

        var updated = NoWarnScrubber.ScrubXunitNoWarns(xml);

        await Assert.That(updated).IsTrue();
        await Assert.That(xml.Descendants("NoWarn").Single().Value).IsEqualTo("$(NoWarn);CS0649;CS8618;CS0105");
    }

    [Test]
    public async Task RemovesElementWhenOnlyXunitWarnings()
    {
        var xml = XDocument.Parse(
            """
            <Project>
              <PropertyGroup>
                <NoWarn>xUnit1013;xUnit1051</NoWarn>
              </PropertyGroup>
            </Project>
            """);

        var updated = NoWarnScrubber.ScrubXunitNoWarns(xml);

        await Assert.That(updated).IsTrue();
        await Assert.That(xml.Descendants("NoWarn")).IsEmpty();
    }

    [Test]
    public async Task KeepsDollarNoWarnWhenXunitRemoved()
    {
        var xml = XDocument.Parse(
            """
            <Project>
              <PropertyGroup>
                <NoWarn>$(NoWarn);xUnit1013</NoWarn>
              </PropertyGroup>
            </Project>
            """);

        var updated = NoWarnScrubber.ScrubXunitNoWarns(xml);

        await Assert.That(updated).IsTrue();
        await Assert.That(xml.Descendants("NoWarn").Single().Value).IsEqualTo("$(NoWarn)");
    }

    [Test]
    public async Task NoChangeWhenNoXunitWarnings()
    {
        var xml = XDocument.Parse(
            """
            <Project>
              <PropertyGroup>
                <NoWarn>$(NoWarn);CS0649;CS8618</NoWarn>
              </PropertyGroup>
            </Project>
            """);

        var updated = NoWarnScrubber.ScrubXunitNoWarns(xml);

        await Assert.That(updated).IsFalse();
        await Assert.That(xml.Descendants("NoWarn").Single().Value).IsEqualTo("$(NoWarn);CS0649;CS8618");
    }

    [Test]
    public async Task IsCaseInsensitive()
    {
        var xml = XDocument.Parse(
            """
            <Project>
              <PropertyGroup>
                <NoWarn>$(NoWarn);XUNIT1013;Xunit1051;xunit1052</NoWarn>
              </PropertyGroup>
            </Project>
            """);

        var updated = NoWarnScrubber.ScrubXunitNoWarns(xml);

        await Assert.That(updated).IsTrue();
        await Assert.That(xml.Descendants("NoWarn").Single().Value).IsEqualTo("$(NoWarn)");
    }

    [Test]
    public async Task HandlesMultipleNoWarnElements()
    {
        var xml = XDocument.Parse(
            """
            <Project>
              <PropertyGroup>
                <NoWarn>$(NoWarn);xUnit1013</NoWarn>
              </PropertyGroup>
              <PropertyGroup>
                <NoWarn>CS0649;xUnit1051</NoWarn>
              </PropertyGroup>
            </Project>
            """);

        var updated = NoWarnScrubber.ScrubXunitNoWarns(xml);

        await Assert.That(updated).IsTrue();
        var noWarns = xml.Descendants("NoWarn").ToList();
        await Assert.That(noWarns).Count().IsEqualTo(2);
        await Assert.That(noWarns[0].Value).IsEqualTo("$(NoWarn)");
        await Assert.That(noWarns[1].Value).IsEqualTo("CS0649");
    }

    [Test]
    public async Task ReturnsFalseWhenNoNoWarnElements()
    {
        var xml = XDocument.Parse(
            """
            <Project>
              <PropertyGroup>
                <TargetFramework>net9.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        var updated = NoWarnScrubber.ScrubXunitNoWarns(xml);

        await Assert.That(updated).IsFalse();
    }
}

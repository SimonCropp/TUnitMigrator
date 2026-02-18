using NUnit.Framework;

[TestFixture]
public class Tests
{
    [Test]
    public void SimpleTest()
    {
        Assert.That(1, Is.EqualTo(1));
    }
}

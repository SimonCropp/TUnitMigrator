public class PackagesMigratorTests
{
    static readonly NuGetVersion tunitVersion = new("1.15.11");

    static async Task<(string result, List<(string OldPackage, string NewPackage)> migrations)> RunMigrate(
        string propsContent,
        TestFramework framework)
    {
        using var tempDir = new TempDirectory();
        var propsPath = Path.Combine(tempDir, "Directory.Packages.props");
        await File.WriteAllTextAsync(propsPath, propsContent);

        var sources = PackageSourceReader.Read(tempDir);
        using var cache = new SourceCacheContext { RefreshMemoryCache = true };

        var migrations = await PackagesMigrator.Migrate(propsPath, framework, tunitVersion, sources, cache);
        var result = await File.ReadAllTextAsync(propsPath);

        return (result, migrations);
    }

    [Test]
    public async Task RemovesMSTestPackages()
    {
        var (result, migrations) = await RunMigrate(
            """
            <Project>
              <PropertyGroup>
                <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
              </PropertyGroup>
              <ItemGroup>
                <PackageVersion Include="MSTest" Version="3.7.3" />
                <PackageVersion Include="MSTest.TestFramework" Version="3.7.3" />
                <PackageVersion Include="MSTest.TestAdapter" Version="3.7.3" />
                <PackageVersion Include="MSTest.Analyzers" Version="3.7.3" />
                <PackageVersion Include="Microsoft.Testing.Extensions.CodeCoverage" Version="17.12.0" />
              </ItemGroup>
            </Project>
            """,
            TestFramework.MSTest);

        await Assert.That(result).DoesNotContain("MSTest.TestFramework");
        await Assert.That(result).DoesNotContain("MSTest.TestAdapter");
        await Assert.That(result).DoesNotContain("MSTest.Analyzers");
        await Assert.That(result).DoesNotContain("Microsoft.Testing.Extensions.CodeCoverage");
        await Assert.That(result).Contains("TUnit");
        await Assert.That(migrations.Where(_ => _.NewPackage == "")).Count().IsGreaterThanOrEqualTo(5);
    }

    [Test]
    public async Task RemovesNUnitPackages()
    {
        var (result, _) = await RunMigrate(
            """
            <Project>
              <PropertyGroup>
                <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
              </PropertyGroup>
              <ItemGroup>
                <PackageVersion Include="NUnit" Version="4.3.2" />
                <PackageVersion Include="NUnit3TestAdapter" Version="4.6.0" />
                <PackageVersion Include="NUnit.ConsoleRunner" Version="3.18.3" />
                <PackageVersion Include="NUnit.Analyzers" Version="4.5.0" />
                <PackageVersion Include="NUnit.Console" Version="3.18.3" />
              </ItemGroup>
            </Project>
            """,
            TestFramework.NUnit);

        await Assert.That(result).DoesNotContain("NUnit");
        await Assert.That(result).DoesNotContain("NUnit3TestAdapter");
        await Assert.That(result).DoesNotContain("NUnit.ConsoleRunner");
        await Assert.That(result).DoesNotContain("NUnit.Analyzers");
        await Assert.That(result).DoesNotContain("NUnit.Console");
        await Assert.That(result).Contains("TUnit");
    }

    [Test]
    public async Task RemovesXunitPackages()
    {
        var (result, _) = await RunMigrate(
            """
            <Project>
              <PropertyGroup>
                <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
              </PropertyGroup>
              <ItemGroup>
                <PackageVersion Include="xunit" Version="2.9.3" />
                <PackageVersion Include="xunit.runner.visualstudio" Version="3.0.2" />
                <PackageVersion Include="xunit.abstractions" Version="2.0.3" />
                <PackageVersion Include="xunit.extensibility.core" Version="2.9.3" />
                <PackageVersion Include="xunit.extensibility.execution" Version="2.9.3" />
              </ItemGroup>
            </Project>
            """,
            TestFramework.Xunit);

        await Assert.That(result).DoesNotContain("xunit");
        await Assert.That(result).DoesNotContain("xunit.runner.visualstudio");
        await Assert.That(result).DoesNotContain("xunit.abstractions");
        await Assert.That(result).DoesNotContain("xunit.extensibility.core");
        await Assert.That(result).DoesNotContain("xunit.extensibility.execution");
        await Assert.That(result).Contains("TUnit");
    }

    [Test]
    public async Task RemovesXunitV3Packages()
    {
        var (result, _) = await RunMigrate(
            """
            <Project>
              <PropertyGroup>
                <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
              </PropertyGroup>
              <ItemGroup>
                <PackageVersion Include="xunit.v3" Version="1.1.0" />
                <PackageVersion Include="xunit.runner.visualstudio" Version="3.0.2" />
              </ItemGroup>
            </Project>
            """,
            TestFramework.XunitV3);

        await Assert.That(result).DoesNotContain("xunit.v3");
        await Assert.That(result).DoesNotContain("xunit.runner.visualstudio");
        await Assert.That(result).Contains("TUnit");
    }

    [Test]
    public async Task RemovesCoverletAndTestSdk()
    {
        var (result, _) = await RunMigrate(
            """
            <Project>
              <PropertyGroup>
                <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
              </PropertyGroup>
              <ItemGroup>
                <PackageVersion Include="xunit" Version="2.9.3" />
                <PackageVersion Include="coverlet.collector" Version="6.0.4" />
                <PackageVersion Include="coverlet.msbuild" Version="6.0.4" />
                <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
                <PackageVersion Include="Microsoft.CodeCoverage" Version="17.12.0" />
                <PackageVersion Include="Microsoft.TestPlatform.ObjectModel" Version="17.12.0" />
                <PackageVersion Include="Microsoft.TestPlatform.TestHost" Version="17.12.0" />
              </ItemGroup>
            </Project>
            """,
            TestFramework.Xunit);

        await Assert.That(result).DoesNotContain("coverlet.collector");
        await Assert.That(result).DoesNotContain("coverlet.msbuild");
        await Assert.That(result).DoesNotContain("Microsoft.NET.Test.Sdk");
        await Assert.That(result).DoesNotContain("Microsoft.CodeCoverage");
        await Assert.That(result).DoesNotContain("Microsoft.TestPlatform.ObjectModel");
        await Assert.That(result).DoesNotContain("Microsoft.TestPlatform.TestHost");
    }

    [Test]
    public async Task AddsTUnitWithVersion()
    {
        var (result, _) = await RunMigrate(
            """
            <Project>
              <PropertyGroup>
                <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
              </PropertyGroup>
              <ItemGroup>
                <PackageVersion Include="NUnit" Version="4.3.2" />
              </ItemGroup>
            </Project>
            """,
            TestFramework.NUnit);

        await Assert.That(result).Contains("""<PackageVersion Include="TUnit" Version="1.15.11" />""");
    }

    [Test]
    public async Task DoesNotDuplicateTUnit()
    {
        var (result, _) = await RunMigrate(
            """
            <Project>
              <PropertyGroup>
                <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
              </PropertyGroup>
              <ItemGroup>
                <PackageVersion Include="NUnit" Version="4.3.2" />
                <PackageVersion Include="TUnit" Version="1.0.0" />
              </ItemGroup>
            </Project>
            """,
            TestFramework.NUnit);

        var tunitCount = result.Split("TUnit").Length - 1;
        await Assert.That(tunitCount).IsEqualTo(1);
    }

    [Test]
    public async Task MigratesExtensionPackage()
    {
        var (result, migrations) = await RunMigrate(
            """
            <Project>
              <PropertyGroup>
                <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
              </PropertyGroup>
              <ItemGroup>
                <PackageVersion Include="MSTest" Version="3.7.3" />
                <PackageVersion Include="Verify.MSTest" Version="31.13.0" />
              </ItemGroup>
            </Project>
            """,
            TestFramework.MSTest);

        await Assert.That(result).DoesNotContain("Verify.MSTest");
        await Assert.That(result).Contains("Verify.TUnit");
        await Assert.That(migrations).Contains(m => m is {OldPackage: "Verify.MSTest", NewPackage: "Verify.TUnit"});
    }

    [Test]
    public async Task PreservesUnrelatedPackages()
    {
        var (result, _) = await RunMigrate(
            """
            <Project>
              <PropertyGroup>
                <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
              </PropertyGroup>
              <ItemGroup>
                <PackageVersion Include="NUnit" Version="4.3.2" />
                <PackageVersion Include="Newtonsoft.Json" Version="13.0.3" />
                <PackageVersion Include="Serilog" Version="4.3.1" />
              </ItemGroup>
            </Project>
            """,
            TestFramework.NUnit);

        await Assert.That(result).Contains("Newtonsoft.Json");
        await Assert.That(result).Contains("Serilog");
    }

    [Test]
    public async Task ScrubsXunitNoWarnsFromProps()
    {
        var (result, _) = await RunMigrate(
            """
            <Project>
              <PropertyGroup>
                <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
                <NoWarn>$(NoWarn);CS0649;CS8618;xUnit1013;xUnit1051</NoWarn>
              </PropertyGroup>
              <ItemGroup>
                <PackageVersion Include="xunit" Version="2.9.3" />
              </ItemGroup>
            </Project>
            """,
            TestFramework.Xunit);

        await Assert.That(result).DoesNotContain("xUnit1013");
        await Assert.That(result).DoesNotContain("xUnit1051");
        await Assert.That(result).Contains("$(NoWarn);CS0649;CS8618");
    }

    [Test]
    public async Task ReturnsMigrationsForRemovedPackages()
    {
        var (_, migrations) = await RunMigrate(
            """
            <Project>
              <PropertyGroup>
                <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
              </PropertyGroup>
              <ItemGroup>
                <PackageVersion Include="xunit" Version="2.9.3" />
                <PackageVersion Include="coverlet.collector" Version="6.0.4" />
              </ItemGroup>
            </Project>
            """,
            TestFramework.Xunit);

        await Assert.That(migrations).Contains(m => m is {OldPackage: "xunit", NewPackage: ""});
        await Assert.That(migrations).Contains(m => m is {OldPackage: "coverlet.collector", NewPackage: ""});
    }
}

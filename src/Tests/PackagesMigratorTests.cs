public class PackagesMigratorTests
{
    static readonly NuGetVersion tunitVersion = new("1.15.11");

    static async Task<(string result, List<(string OldPackage, string NewPackage)> migrations)> RunMigrate(
        string propsContent)
    {
        using var tempDir = new TempDirectory();
        var propsPath = Path.Combine(tempDir, "Directory.Packages.props");
        await File.WriteAllTextAsync(propsPath, propsContent);

        var sources = PackageSourceReader.Read(tempDir);
        using var cache = new SourceCacheContext { RefreshMemoryCache = true };

        var migrations = await PackagesMigrator.Migrate(propsPath, tunitVersion, sources, cache);
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
            """);

        await Verify(result);
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
            """);

        await Verify(result);
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
            """);

        await Verify(result);
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
            """);

        await Verify(result);
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
            """);

        await Verify(result);
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
            """);

        await Verify(result);
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
            """);

        await Verify(result);
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
            """);

        await Verify(result);
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
            """);

        await Verify(result);
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
            """);

        await Verify(result);
    }

    [Test]
    public async Task RemovesPackageReferencesInConditionalItemGroup()
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
                <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
              </ItemGroup>
              <ItemGroup Condition="'$(IsTestProject)' == 'true'">
                <PackageReference Include="xunit" />
                <PackageReference Include="coverlet.collector" />
                <PackageReference Include="Microsoft.NET.Test.Sdk" />
              </ItemGroup>
            </Project>
            """);

        await Verify(result);
    }

    [Test]
    public async Task RenamesExtensionPackageReferencesInConditionalItemGroup()
    {
        var (result, _) = await RunMigrate(
            """
            <Project>
              <PropertyGroup>
                <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
              </PropertyGroup>
              <ItemGroup>
                <PackageVersion Include="MSTest" Version="3.7.3" />
                <PackageVersion Include="Verify.MSTest" Version="31.13.0" />
              </ItemGroup>
              <ItemGroup Condition="'$(IsTestProject)' == 'true'">
                <PackageReference Include="MSTest" />
                <PackageReference Include="Verify.MSTest" />
              </ItemGroup>
            </Project>
            """);

        await Verify(result);
    }

    [Test]
    public async Task PreservesUnrelatedPackageReferencesInConditionalItemGroup()
    {
        var (result, _) = await RunMigrate(
            """
            <Project>
              <PropertyGroup>
                <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
              </PropertyGroup>
              <ItemGroup>
                <PackageVersion Include="xunit" Version="2.9.3" />
                <PackageVersion Include="Newtonsoft.Json" Version="13.0.3" />
              </ItemGroup>
              <ItemGroup Condition="'$(IsTestProject)' == 'true'">
                <PackageReference Include="xunit" />
                <PackageReference Include="Newtonsoft.Json" />
              </ItemGroup>
            </Project>
            """);

        await Verify(result);
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
            """);

        await Assert.That(migrations).Contains(m => m is {OldPackage: "xunit", NewPackage: ""});
        await Assert.That(migrations).Contains(m => m is {OldPackage: "coverlet.collector", NewPackage: ""});
    }
}

public class CsprojMigratorTests
{
    [Test]
    public async Task RemovesPackageAndAddsTUnit()
    {
        using var tempDir = new TempDirectory();
        var csproj = Path.Combine(tempDir, "Test.csproj");
        await File.WriteAllTextAsync(csproj,
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="xunit" />
                <PackageReference Include="Verify.Xunit" />
              </ItemGroup>
            </Project>
            """);

        List<(string OldPackage, string NewPackage)> migrations =
        [
            ("xunit", ""),
            ("Verify.Xunit", "Verify.TUnit")
        ];

        await CsprojMigrator.Migrate(tempDir, migrations);

        var result = await File.ReadAllTextAsync(csproj);
        await Assert.That(result).DoesNotContain("xunit");
        await Assert.That(result).Contains("Verify.TUnit");
        await Assert.That(result).Contains("""<PackageReference Include="TUnit" />""");
    }

    [Test]
    public async Task DoesNotAddTUnitWhenAlreadyPresent()
    {
        using var tempDir = new TempDirectory();
        var csproj = Path.Combine(tempDir, "Test.csproj");
        await File.WriteAllTextAsync(csproj,
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="NUnit" />
                <PackageReference Include="TUnit" />
              </ItemGroup>
            </Project>
            """);

        List<(string OldPackage, string NewPackage)> migrations = [("NUnit", "")];

        await CsprojMigrator.Migrate(tempDir, migrations);

        var result = await File.ReadAllTextAsync(csproj);
        var tunitCount = result.Split("TUnit").Length - 1;
        await Assert.That(tunitCount).IsEqualTo(1);
    }

    [Test]
    public async Task RenamesExtensionPackage()
    {
        using var tempDir = new TempDirectory();
        var csproj = Path.Combine(tempDir, "Test.csproj");
        await File.WriteAllTextAsync(csproj,
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Verify.MSTest" />
              </ItemGroup>
            </Project>
            """);

        List<(string OldPackage, string NewPackage)> migrations = [("Verify.MSTest", "Verify.TUnit")];

        await CsprojMigrator.Migrate(tempDir, migrations);

        var result = await File.ReadAllTextAsync(csproj);
        await Assert.That(result).DoesNotContain("Verify.MSTest");
        await Assert.That(result).Contains("Verify.TUnit");
        // Rename only — no TUnit PackageReference added
        await Assert.That(result).DoesNotContain("""<PackageReference Include="TUnit" />""");
    }

    [Test]
    public async Task LeavesUnrelatedReferencesUntouched()
    {
        using var tempDir = new TempDirectory();
        var csproj = Path.Combine(tempDir, "Test.csproj");
        await File.WriteAllTextAsync(csproj,
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Newtonsoft.Json" />
                <PackageReference Include="xunit" />
              </ItemGroup>
            </Project>
            """);

        List<(string OldPackage, string NewPackage)> migrations = [("xunit", "")];

        await CsprojMigrator.Migrate(tempDir, migrations);

        var result = await File.ReadAllTextAsync(csproj);
        await Assert.That(result).Contains("Newtonsoft.Json");
        await Assert.That(result).DoesNotContain("xunit");
    }

    [Test]
    public async Task NoChangesWhenNoMigrations()
    {
        using var tempDir = new TempDirectory();
        var csproj = Path.Combine(tempDir, "Test.csproj");
        var original =
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Newtonsoft.Json" />
              </ItemGroup>
            </Project>
            """;
        await File.WriteAllTextAsync(csproj, original);

        List<(string OldPackage, string NewPackage)> migrations = [("xunit", "")];

        await CsprojMigrator.Migrate(tempDir, migrations);

        var result = await File.ReadAllTextAsync(csproj);
        await Assert.That(result).IsEqualTo(original);
    }

    [Test]
    public async Task ScrubsXunitNoWarnsFromCsproj()
    {
        using var tempDir = new TempDirectory();
        var csproj = Path.Combine(tempDir, "Test.csproj");
        await File.WriteAllTextAsync(csproj,
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <NoWarn>$(NoWarn);CS0649;CS8618;CS0105;xUnit1013;xUnit1051</NoWarn>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="xunit" />
              </ItemGroup>
            </Project>
            """);

        List<(string OldPackage, string NewPackage)> migrations = [("xunit", "")];

        await CsprojMigrator.Migrate(tempDir, migrations);

        var result = await File.ReadAllTextAsync(csproj);
        await Assert.That(result).DoesNotContain("xUnit1013");
        await Assert.That(result).DoesNotContain("xUnit1051");
        await Assert.That(result).Contains("$(NoWarn);CS0649;CS8618;CS0105");
    }

    [Test]
    public async Task ScrubsXunitNoWarnsEvenWithNoMigrations()
    {
        using var tempDir = new TempDirectory();
        var csproj = Path.Combine(tempDir, "Test.csproj");
        await File.WriteAllTextAsync(csproj,
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <NoWarn>$(NoWarn);xUnit1013;xUnit1051</NoWarn>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Newtonsoft.Json" />
              </ItemGroup>
            </Project>
            """);

        List<(string OldPackage, string NewPackage)> migrations = [];

        await CsprojMigrator.Migrate(tempDir, migrations);

        var result = await File.ReadAllTextAsync(csproj);
        await Assert.That(result).DoesNotContain("xUnit1013");
        await Assert.That(result).DoesNotContain("xUnit1051");
    }

    [Test]
    public async Task HandlesNestedCsprojFiles()
    {
        using var tempDir = new TempDirectory();
        var subDir = Path.Combine(tempDir, "src", "Tests");
        Directory.CreateDirectory(subDir);
        var csproj = Path.Combine(subDir, "Tests.csproj");
        await File.WriteAllTextAsync(csproj,
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="MSTest" />
              </ItemGroup>
            </Project>
            """);

        List<(string OldPackage, string NewPackage)> migrations = [("MSTest", "")];

        await CsprojMigrator.Migrate(tempDir, migrations);

        var result = await File.ReadAllTextAsync(csproj);
        await Assert.That(result).DoesNotContain("MSTest");
        await Assert.That(result).Contains("""<PackageReference Include="TUnit" />""");
    }
}

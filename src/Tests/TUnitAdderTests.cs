public class TUnitAdderTests
{
    [Test]
    public async Task AddsOutputTypeWhenMissing()
    {
        using var tempDir = new TempDirectory();
        var csproj = Path.Combine(tempDir, "Test.csproj");
        await File.WriteAllTextAsync(csproj,
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="xunit" />
              </ItemGroup>
            </Project>
            """);

        await TUnitAdder.AddToCsprojs(tempDir);

        var result = await File.ReadAllTextAsync(csproj);
        await Assert.That(result).Contains("<OutputType>Exe</OutputType>");
    }

    [Test]
    public async Task SetsOutputTypeToExeWhenDifferentValue()
    {
        using var tempDir = new TempDirectory();
        var csproj = Path.Combine(tempDir, "Test.csproj");
        await File.WriteAllTextAsync(csproj,
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <OutputType>Library</OutputType>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="NUnit" />
              </ItemGroup>
            </Project>
            """);

        await TUnitAdder.AddToCsprojs(tempDir);

        var result = await File.ReadAllTextAsync(csproj);
        await Assert.That(result).Contains("<OutputType>Exe</OutputType>");
        await Assert.That(result).DoesNotContain("Library");
    }

    [Test]
    public async Task LeavesOutputTypeWhenAlreadyExe()
    {
        using var tempDir = new TempDirectory();
        var csproj = Path.Combine(tempDir, "Test.csproj");
        await File.WriteAllTextAsync(csproj,
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <OutputType>Exe</OutputType>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="MSTest" />
              </ItemGroup>
            </Project>
            """);

        await TUnitAdder.AddToCsprojs(tempDir);

        var result = await File.ReadAllTextAsync(csproj);
        var outputTypeCount = result.Split("OutputType").Length - 1;
        await Assert.That(outputTypeCount).IsEqualTo(2); // open + close tags
    }

    [Test]
    public async Task DoesNotAddOutputTypeToNonTestCsprojs()
    {
        using var tempDir = new TempDirectory();
        var csproj = Path.Combine(tempDir, "App.csproj");
        await File.WriteAllTextAsync(csproj,
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Newtonsoft.Json" />
              </ItemGroup>
            </Project>
            """);

        await TUnitAdder.AddToCsprojs(tempDir);

        var result = await File.ReadAllTextAsync(csproj);
        await Assert.That(result).DoesNotContain("OutputType");
        await Assert.That(result).DoesNotContain("TUnit");
    }
}

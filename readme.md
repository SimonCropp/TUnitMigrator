# <img src="/src/icon.png" height="30px"> TUnitMigrator

[![Build status](https://img.shields.io/appveyor/build/SimonCropp/TUnitMigrator)](https://ci.appveyor.com/project/SimonCropp/TUnitMigrator)
[![NuGet Status](https://img.shields.io/nuget/v/TUnitMigrator.svg)](https://www.nuget.org/packages/TUnitMigrator/)

A .NET tool that migrates test projects from MSTest, NUnit, xUnit, and xUnit v3 to [TUnit](https://github.com/thomhurst/TUnit).

## Installation

```bash
dotnet tool install --global TUnitMigrator
```

## Usage

```bash
# Migrate a single repo
tunit-migrate /path/to/repo

# Migrate all repos in a directory
tunit-migrate /path/to/parent-directory

# Or use the target-directory option
tunit-migrate -t /path/to/repo
```

## What It Does


### 1. Package Removal

Removes the following NuGet packages from `Directory.Packages.props`:

| MSTest | NUnit | xUnit | xUnit v3 |
|--------|-------|-------|----------|
| `MSTest` | `NUnit` | `xunit` | `xunit.v3` |
| `MSTest.TestFramework` | `NUnit3TestAdapter` | `xunit.runner.visualstudio` | `xunit.runner.visualstudio` |
| `MSTest.TestAdapter` | `NUnit.ConsoleRunner` | `xunit.abstractions` | `xunit.abstractions` |
| `MSTest.Analyzers` | `NUnit.Analyzers` | `xunit.extensibility.*` | `xunit.extensibility.*` |
| `Microsoft.Testing.*` | `NUnit.Console` | | |

**Always removed** (all frameworks):
- `coverlet.collector`, `coverlet.msbuild` — [Coverlet is unnecessary](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-extensions-code-coverage) because Microsoft.Testing.Platform (used by TUnit) has built-in code coverage support via `--coverage`
- `Microsoft.NET.Test.Sdk` — the VSTest runner glue, replaced by [Microsoft.Testing.Platform](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-intro)
- `Microsoft.CodeCoverage` — replaced by built-in coverage support
- `Microsoft.TestPlatform.*` — VSTest platform packages, no longer needed


### 2. TUnit Package Addition

Adds the latest stable version of the `TUnit` package to `Directory.Packages.props`.


### 3. Extension Package Discovery

For any remaining package whose name ends with `.MSTest`, `.NUnit`, `.Xunit`, or `.XunitV3`, the tool:

1. Replaces the suffix with `.TUnit`
2. Checks if the `.TUnit` package exists on NuGet
3. If it does, migrates to the `.TUnit` variant with the latest stable version

Example: `Verify.MSTest` → `Verify.TUnit`


### 4. `.csproj` PackageReference Updates

For each `.csproj` file:
- Removes `<PackageReference>` entries for removed packages
- Renames `<PackageReference>` entries for migrated extension packages
- Adds `<PackageReference Include="TUnit" />` if any test references were modified
- Sets `<OutputType>Exe</OutputType>` on test projects (required by TUnit). If the element is missing it is added to the first `<PropertyGroup>`; if present with a different value it is changed to `Exe`
- Scrubs xUnit `NoWarn` suppressions from `<NoWarn>` elements (e.g. `xUnit1013`, `xUnit1051`). If all entries in a `<NoWarn>` element are xUnit warnings, the entire element is removed


### 5. YML CI File Migration

Transforms `dotnet test` commands in `.yml` files:
```yaml
# Before
- run: dotnet test src --configuration Release

# After
- run: dotnet test --solution src/MySolution.slnx --configuration Release
```

The tool finds the first `.slnx` or `.sln` file in the referenced directory.


### 6. `global.json` Relocation

If no `global.json` exists, one is created at the project root. If exactly one is found and it's not at the root, it's moved to the root. When relocated:

- References to the old `global.json` path in `.yml` files are updated
- References to the old `global.json` path in `.sln` and `.slnx` solution files are patched to point to the new location (`.slnx` uses forward slashes, `.sln` uses backslashes)


### 7. C# Source Code Migration

The tool automatically runs `dotnet format analyzers` with the appropriate TUnit diagnostic to migrate C# source code (attributes, assertions, etc.):

| Framework | Diagnostic |
|-----------|-----------|
| MSTest | `TUMS0001` |
| NUnit | `TUNU0001` |
| xUnit / xUnit v3 | `TUXU0001` |

This step runs after TUnit is added but before old framework packages are removed, so the project can still compile during analysis.


## Requirements

- **Central Package Management (CPM)**: Projects must use a `Directory.Packages.props` file with `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>`.
- Each git repository (identified by a `.git` directory) is treated as one migration unit.


## Post-Migration

Some patterns may not be automatically migrated. Review the output for any remaining manual changes needed. See the official TUnit migration guides below for detailed guidance.


## Official TUnit Migration Guides

For detailed guidance on migrating C# test code (attributes, assertions, lifecycle hooks, etc.), see the official TUnit documentation:

- [Migrating from MSTest](https://tunit.dev/docs/migration/mstest)
- [Migrating from NUnit](https://tunit.dev/docs/migration/nunit)
- [Migrating from xUnit](https://tunit.dev/docs/migration/xunit)
- [Framework Differences](https://tunit.dev/docs/comparison/framework-differences)

# TUnitMigrator

A .NET global tool that migrates test projects from MSTest, NUnit, xUnit, and xUnit v3 to [TUnit](https://github.com/thomhurst/TUnit).

## Installation

```bash
dotnet tool install -g TUnitMigrator
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
| `MSTest.TestAdapter` | | | |

**Always removed** (all frameworks):
- `coverlet.collector`, `coverlet.msbuild` — Coverlet is unnecessary because Microsoft.Testing.Platform (used by TUnit) has built-in code coverage support via `--collect-code-coverage` / `--coverage`
- `Microsoft.NET.Test.Sdk` — the VSTest runner glue, replaced by Microsoft.Testing.Platform


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

If exactly one `global.json` is found in the project and it's not at the root, it's moved to the root.


## Requirements

- **Central Package Management (CPM)**: Projects must use a `Directory.Packages.props` file with `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>`.
- Each git repository (identified by a `.git` directory) is treated as one migration unit.


## Post-Migration

After running the tool, use TUnit's Roslyn analyzers to migrate C# source code (attributes, assertions, etc.):

```bash
dotnet format analyzers --severity info --diagnostics TUMS0001 TUNU0001 TUXU0001
```

This handles the actual C# code transformation — the migrator tool focuses exclusively on project infrastructure.


## Official TUnit Migration Guides

For detailed guidance on migrating C# test code (attributes, assertions, lifecycle hooks, etc.), see the official TUnit documentation:

- [Migrating from MSTest](https://tunit.dev/docs/migration/mstest)
- [Migrating from NUnit](https://tunit.dev/docs/migration/nunit)
- [Migrating from xUnit](https://tunit.dev/docs/migration/xunit)
- [Framework Differences](https://tunit.dev/docs/comparison/framework-differences)

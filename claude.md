# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

`tunit-migrate` is a .NET global tool (`src/TUnitMigrator/Program.cs`) that migrates a repo's test projects from MSTest/NUnit/xUnit/xUnit v3 to TUnit. Packaged as `PackAsTool` in `TUnitMigrator.csproj`.

## Build & Test

```bash
dotnet build src
dotnet test --solution src/TUnitMigrator.slnx

# Run a single test (Microsoft.Testing.Platform — note: --treenode-filter, NOT --filter)
dotnet test --solution src/TUnitMigrator.slnx --treenode-filter "/*/*/MigratorTests/Migration(framework: MSTest)"
```

## Architecture

- **Migrator.cs** — Top-level orchestrator. Discovers project roots via `.git` marker directories, then runs all migrators.
- **FrameworkDetector.cs** — Checks if `Directory.Packages.props` contains any known test framework package (prefix matching on MSTest/NUnit/xunit).
- **PackagesMigrator.cs** — Modifies `Directory.Packages.props`: removes old framework packages (by prefix matching and exact match), adds TUnit, resolves extension packages, scrubs xUnit `NoWarn` suppressions.
- **CsprojMigrator.cs** — Updates `.csproj` `PackageReference` entries based on migrations from PackagesMigrator. Also scrubs xUnit `NoWarn` suppressions.
- **NoWarnScrubber.cs** — Removes xUnit warning suppressions (e.g. `xUnit1013`) from `<NoWarn>` elements in XML documents.
- **YmlMigrator.cs** — Rewrites `dotnet test <dir>` to `dotnet test --solution <dir>/SolutionFile` in `.yml` files.
- **GlobalJsonRelocator.cs** — Creates `global.json` at root if none exists, or moves it to root if found in a subdirectory. Patches paths in `.yml`, `.sln`, and `.slnx` files. Ensures `Microsoft.Testing.Platform` test runner is configured.
- **CodeMigrator.cs** — Runs `dotnet format analyzers` with all TUnit diagnostics (TUMS0001/TUNU0001/TUXU0001) to migrate C# source code.
- **TUnitAdder.cs** — Adds TUnit to `Directory.Packages.props` and `.csproj` files before code migration (so the project still compiles). Detects test projects by prefix matching on package references. Also ensures `<OutputType>Exe</OutputType>` is set on test csprojs.
- **ExtensionPackageResolver.cs** — Maps `.MSTest`/`.NUnit`/`.Xunit`/`.XunitV3` suffixes to `.TUnit`.
- **NuGetPackageChecker.cs** — Queries NuGet for package existence and latest stable version.
- **XmlHelper.cs** — Format-preserving XML read/write (newline detection, trailing newline).

## Migration Order

1. Add TUnit to props + csprojs (TUnitAdder)
2. Run `dotnet format analyzers` for C# code migration (CodeMigrator)
3. Remove old framework packages from props (PackagesMigrator)
4. Update csproj references (CsprojMigrator)
5. Rewrite yml CI files (YmlMigrator)
6. Relocate global.json (GlobalJsonRelocator)

## Conventions

- Uses Central Package Management (CPM) — `Directory.Packages.props` is required.
- Each git repo (identified by `.git` directory) is one migration unit.
- Follow the patterns in `C:\Code\PackageUpdate` for code style.

## Tests

- **`src/Scenarios/`** — Per-framework fixture repos (`MSTestScenario`, `NUnitScenario`, `XunitScenario`, `XunitV3Scenario`, `TUnitScenario`). `MigratorTests` copies a scenario to a `TempDirectory`, creates a `.git` marker, runs `Migrator.Migrate`, then Verifies the resulting props/csproj/yml/global.json/tests.cs.
- Uses **Verify** snapshot testing. A failing test writes `*.received.txt`; approve by replacing the matching `*.verified.txt` (DiffEngine typically opens a diff tool).
- Scenario `.git` dirs are not checked in (nested repos can't be tracked) — tests create them at runtime.

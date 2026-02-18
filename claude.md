# TUnitMigrator

## Build & Test

```bash
dotnet build src
dotnet test --solution src/TUnitMigrator.slnx
```

## Architecture

- **Migrator.cs** — Top-level orchestrator. Discovers project roots via `.git` marker directories, then runs all migrators.
- **FrameworkDetector.cs** — Detects MSTest/NUnit/xUnit/xUnitV3 from `Directory.Packages.props`.
- **PackagesMigrator.cs** — Modifies `Directory.Packages.props`: removes old framework packages, adds TUnit, resolves extension packages.
- **CsprojMigrator.cs** — Updates `.csproj` `PackageReference` entries based on migrations from PackagesMigrator.
- **YmlMigrator.cs** — Rewrites `dotnet test <dir>` to `dotnet test --solution <dir>/SolutionFile` in `.yml` files.
- **GlobalJsonRelocator.cs** — Moves `global.json` to project root if exactly one found.
- **ExtensionPackageResolver.cs** — Maps `.MSTest`/`.NUnit`/`.Xunit`/`.XunitV3` suffixes to `.TUnit`.
- **NuGetPackageChecker.cs** — Queries NuGet for package existence and latest stable version.
- **XmlHelper.cs** — Format-preserving XML read/write (newline detection, trailing newline).

## Conventions

- Uses Central Package Management (CPM) — `Directory.Packages.props` is required.
- Each git repo (identified by `.git` directory) is one migration unit.
- Does NOT rewrite C# source code — TUnit's Roslyn analyzers handle that.
- Follow the patterns in `C:\Code\PackageUpdate` for code style.

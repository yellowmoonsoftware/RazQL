# Repository Guidelines

## Project Structure & Module Organization

This repository contains the RazQL .NET libraries and their tests.

- `RazQL.slnx` includes all production and test projects.
- `src/RazQL/` contains the provider-neutral runtime assembly packaged as `RazQL.Core`. Consumer-facing types live in the root namespace; `Binding/`, `Execution/`, and `Template/` group their subsystems.
- `src/RazQL.Dapper/` contains the Dapper execution adapter and conversion boundary.
- `src/RazQL.Package/` produces the dependency-only `RazQL` metapackage used for standard installation.
- `src/RazQL.Generators/` contains mapper discovery, analysis, validation, and source emission.
- `src/RazQL.DependencyInjection/` integrates the runtime and generated mappers with Microsoft dependency injection.
- `tests/` mirrors the runtime, adapter, DI, and generator projects. SQL fixtures live below the relevant test project.
- `Directory.Packages.props` centrally manages NuGet versions; project references must remain versionless.

## Build, Test, and Development Commands

Use the .NET 10 SDK from the repository root:

- `dotnet restore RazQL.slnx` restores dependencies.
- `dotnet build RazQL.slnx` compiles every project and runs generator diagnostics.
- `dotnet test RazQL.slnx` runs the complete xUnit suite.
- `dotnet test tests/RazQL.Tests/RazQL.Tests.csproj --collect:"XPlat Code Coverage"` collects runtime-library coverage.
- `dotnet pack RazQL.slnx -c Release` creates all distributable packages for inspection.
- `./eng/test-packages.sh` packs every distributable project and verifies a clean package-only consumer.

## Coding Style & Naming Conventions

Use four-space indentation, braces on separate lines, file-scoped namespaces, and nullable reference types. Use PascalCase for types and methods, camelCase for parameters and locals, and an `I` prefix for interfaces. Match namespaces to directories. Keep branded consumer APIs in `RazQL`; use subsystem namespaces for implementation contracts. Prefer explicit null handling over suppressions. Avoid unrelated formatting changes.

## Testing Guidelines

Use xUnit and NSubstitute. Group tests by subsystem and name cases `Method_ExpectedBehavior`, such as `Bind_AddsNamedParameterAndReusesIt`. Mock nontrivial dependencies through interfaces. Keep unit tests deterministic and independent of databases, files outside test fixtures, and network services.

## Commit & Pull Request Guidelines

Use Conventional Commits, such as `feat: emit mapper implementations`, `fix: resolve resource templates`, and `docs: explain template conventions`. Keep commits focused. Pull requests should explain the problem, resulting behavior, API or diagnostic changes, and validation performed.

## Security

Treat Razor templates as trusted executable application code. Never commit credentials, local configuration, build output, packages, or personal IDE settings.

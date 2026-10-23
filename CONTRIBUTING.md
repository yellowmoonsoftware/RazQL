# Contributing to RazQL

Bug reports, feature proposals, documentation improvements, tests, and code fixes
are welcome. Participation is governed by the [Code of Conduct](CODE_OF_CONDUCT.md).

## Before Opening an Issue

Search existing issues first. Use the bug or feature template to describe the
problem, expected behavior, affected package and version, and a minimal example
when possible. Remove credentials and other sensitive data from logs. Report
suspected vulnerabilities privately as described in [SECURITY.md](SECURITY.md).

For substantial API or behavior changes, open an issue before starting work so
compatibility and approach can be discussed. Small fixes may go directly to a
pull request.

## Development Setup

Use .NET SDK 10.0.100 or newer within the .NET 10 line. From the repository root:

```shell
dotnet restore RazQL.slnx
dotnet build RazQL.slnx
dotnet test RazQL.slnx
./eng/test-packages.sh
```

The last command packs all distributable projects and runs a clean consumer using
only the resulting NuGet packages. It also renders an embedded Razor template and
checks the generated SQL and bound parameter. It is especially important for
generator, package, and dependency-injection changes.

## Code and Tests

Follow the existing four-space C# style, file-scoped namespaces, nullable
references, and directory-matched namespaces. Add focused xUnit tests for
behavioral changes; mock nontrivial dependencies through their interfaces.
Changes to public APIs should include XML documentation. Update the relevant
README and diagnostic documentation when changing usage or compiler feedback.
Treat Razor templates as trusted executable code, and never commit secrets.

## Commits and Pull Requests

Use [Conventional Commits](https://www.conventionalcommits.org/), such as
`fix: resolve resource templates` or `feat: emit mapper implementations`.
Mark breaking changes with `!` and explain their migration impact. Keep pull
requests focused, link related issues, describe user-visible behavior and
compatibility effects, and list the validation performed. Contributions are
accepted under the repository's [MIT License](LICENSE).

## Releases

The Release Please workflow uses Conventional Commits on `main` to open a
release pull request. It updates `CHANGELOG.md`, the release manifest, and the
shared version in `src/Directory.Build.props`; all five packages use that version.
The release pull request receives the same CI checks as other changes. Merge it only
after its proposed version and all checks have been verified. Merging it creates a
version tag and a non-prerelease GitHub release. The tag workflow verifies that the
tag matches the shared project version, builds and tests the solution, checks package
consumption, and publishes all five packages to NuGet.org through Trusted Publishing.
Package and runtime symbol-package artifacts are also attached to the GitHub release.

Non-alpha releases start at `0.1.0`; the older alpha tags remain part of the history.
During `0.x` development, features and breaking changes increment the minor version,
and compatible fixes increment the patch version. Release Please uses its default
versioning strategy with `bump-minor-pre-major` enabled. A deliberate one-time version
override belongs in a merged commit's `Release-As: <version>` footer; do not leave a
permanent `release-as` setting in the configuration. The release manifest records the
last released version and should be updated by the release PR, not in advance.

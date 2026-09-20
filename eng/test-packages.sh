#!/usr/bin/env bash

set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
configuration="${CONFIGURATION:-Release}"
package_version="${PACKAGE_VERSION:-0.0.0-package-tests.$(date +%s)}"
working_directory="$(mktemp -d "${TMPDIR:-/tmp}/razql-package-tests.XXXXXX")"
package_directory="$working_directory/packages"
consumer_directory="$working_directory/consumer"
nuget_packages="$working_directory/nuget-packages"
nuget_config="$working_directory/NuGet.config"

cleanup() {
    rm -rf "$working_directory"
}
trap cleanup EXIT

mkdir -p "$package_directory" "$consumer_directory" "$nuget_packages"

cat > "$nuget_config" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$package_directory" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
EOF

dotnet restore "$repository_root/RazQL.slnx"

projects=(
    "src/RazQL/RazQL.csproj"
    "src/RazQL.Generators/RazQL.Generators.csproj"
    "src/RazQL.Dapper/RazQL.Dapper.csproj"
    "src/RazQL.DependencyInjection/RazQL.DependencyInjection.csproj"
    "src/RazQL.Package/RazQL.Package.csproj"
)

for project in "${projects[@]}"; do
    dotnet pack "$repository_root/$project" \
        --configuration "$configuration" \
        --no-restore \
        --output "$package_directory" \
        -p:PackageVersion="$package_version"
done

symbol_package_ids=(
    "RazQL.Core"
    "RazQL.Dapper"
    "RazQL.DependencyInjection"
)

for package_id in "${symbol_package_ids[@]}"; do
    symbol_package="$package_directory/$package_id.$package_version.snupkg"
    if [[ ! -f "$symbol_package" ]]; then
        echo "Expected symbol package was not created: $symbol_package" >&2
        exit 1
    fi
done

cp -R "$repository_root/tests/PackageConsumption/Consumer/." "$consumer_directory/"

NUGET_PACKAGES="$nuget_packages" dotnet restore \
    "$consumer_directory/RazQL.PackageConsumer.csproj" \
    --configfile "$nuget_config" \
    -p:RazQLPackageVersion="$package_version"

NUGET_PACKAGES="$nuget_packages" dotnet run \
    --project "$consumer_directory/RazQL.PackageConsumer.csproj" \
    --configuration "$configuration" \
    --no-restore \
    -p:RazQLPackageVersion="$package_version" \
    -- "$package_directory" "$package_version"

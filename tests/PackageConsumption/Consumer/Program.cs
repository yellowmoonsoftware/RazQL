using System.IO.Compression;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using RazQL;
using RazQL.Dapper;
using RazQL.DependencyInjection;
using RazQL.Execution;
using RazQL.PackageConsumption.Mapper;

if (args.Length != 2)
{
    throw new InvalidOperationException("Expected the package directory and package version.");
}

var packageDirectory = args[0];
var packageVersion = args[1];
var consumerAssembly = typeof(IPackageMapper).Assembly;
var mapping = consumerAssembly
    .GetCustomAttributes<RazQLMapperImplementationAttribute>()
    .Single(attribute => attribute.MapperType == typeof(IPackageMapper));

var services = new ServiceCollection();
services.AddRazQL(builder => builder
    .UsingExecutionAdapter<DapperExecutionAdapter>()
    .AddMappersFromAssembly(consumerAssembly));

var adapterRegistration = services.Single(descriptor => descriptor.ServiceType == typeof(IExecutionAdapter));
if (adapterRegistration.ImplementationType != typeof(DapperExecutionAdapter))
{
    throw new InvalidOperationException("Dapper execution adapter was not registered.");
}

var mapperRegistration = services.Single(descriptor => descriptor.ServiceType == typeof(IPackageMapper));
if (mapperRegistration.ImplementationType != mapping.ImplementationType)
{
    throw new InvalidOperationException("Generated mapper registration was not discovered by dependency injection.");
}

var preloaderRegistration = services.Single(
    descriptor => descriptor.ServiceType == typeof(IMapperTemplatePreloader));
if (preloaderRegistration.ImplementationFactory is null)
{
    throw new InvalidOperationException("Generated mapper preloader registration was not discovered.");
}

AssertPackageEntries(
    "RazQL",
    ["README.md"],
    forbiddenPrefixes: ["lib/"]);
AssertPackageEntries(
    "RazQL.Core",
    ["README.md", "lib/net10.0/RazQL.dll", "lib/net10.0/RazQL.xml"]);
AssertPackageEntries(
    "RazQL.Dapper",
    ["README.md", "lib/net10.0/RazQL.Dapper.dll", "lib/net10.0/RazQL.Dapper.xml"]);
AssertPackageEntries(
    "RazQL.DependencyInjection",
    [
        "README.md",
        "lib/net10.0/RazQL.DependencyInjection.dll",
        "lib/net10.0/RazQL.DependencyInjection.xml"
    ]);
AssertPackageEntries(
    "RazQL.Generators",
    [
        "README.md",
        "analyzers/dotnet/cs/RazQL.Generators.dll",
        "buildTransitive/RazQL.Generators.targets"
    ],
    forbiddenPrefixes: ["lib/"]);

AssertPackageDependencies("RazQL", ["RazQL.Core", "RazQL.Generators", "RazQL.Dapper"]);
AssertPackageDependencies("RazQL.Core", [], ["Dapper"]);
AssertPackageDependencies("RazQL.Dapper", ["RazQL.Core", "Dapper"]);
AssertPackageDependencies("RazQL.DependencyInjection", ["RazQL.Core"], ["Dapper", "RazQL.Dapper"]);

Console.WriteLine("RazQL package-consumption verification passed.");
return;

void AssertPackageEntries(
    string packageId,
    IReadOnlyCollection<string> requiredEntries,
    IReadOnlyCollection<string>? forbiddenPrefixes = null)
{
    var packagePath = Path.Combine(packageDirectory, $"{packageId}.{packageVersion}.nupkg");
    using var archive = ZipFile.OpenRead(packagePath);
    var entries = archive.Entries
        .Select(entry => entry.FullName)
        .ToHashSet(StringComparer.Ordinal);

    foreach (var requiredEntry in requiredEntries)
    {
        if (!entries.Contains(requiredEntry))
        {
            throw new InvalidOperationException(
                $"Package '{packageId}' does not contain required entry '{requiredEntry}'.");
        }
    }

    foreach (var forbiddenPrefix in forbiddenPrefixes ?? [])
    {
        if (entries.Any(entry => entry.StartsWith(forbiddenPrefix, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                $"Package '{packageId}' contains forbidden path '{forbiddenPrefix}'.");
        }
    }
}

void AssertPackageDependencies(string packageId, IReadOnlyCollection<string> required,
    IReadOnlyCollection<string>? forbidden = null)
{
    var packagePath = Path.Combine(packageDirectory, $"{packageId}.{packageVersion}.nupkg");
    using var archive = ZipFile.OpenRead(packagePath);
    using var nuspecStream = archive.GetEntry($"{packageId}.nuspec")?.Open() ??
                             throw new InvalidOperationException($"Package '{packageId}' has no nuspec.");
    var dependencies = XDocument.Load(nuspecStream)
        .Descendants()
        .Where(element => element.Name.LocalName == "dependency")
        .Select(element => (string?)element.Attribute("id"))
        .OfType<string>()
        .ToHashSet(StringComparer.Ordinal);

    foreach (var dependency in required)
    {
        if (!dependencies.Contains(dependency))
            throw new InvalidOperationException($"Package '{packageId}' is missing dependency '{dependency}'.");
    }

    foreach (var dependency in forbidden ?? [])
    {
        if (dependencies.Contains(dependency))
            throw new InvalidOperationException($"Package '{packageId}' unexpectedly depends on '{dependency}'.");
    }
}

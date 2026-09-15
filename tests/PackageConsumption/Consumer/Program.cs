using System.IO.Compression;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using RazQL;
using RazQL.DependencyInjection;
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
services.AddRazQL(builder => builder.AddMappersFromAssembly(consumerAssembly));

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

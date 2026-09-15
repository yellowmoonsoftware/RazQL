using Microsoft.CodeAnalysis;
using RazQL.Generators.Models;

namespace RazQL.Generators.Validation;

internal static class MapperQueryGroupNameValidator
{
    public static IEnumerable<Diagnostic> Validate(IReadOnlyList<MapperModel> mappers)
    {
        var conflicts = new Dictionary<MapperModel, HashSet<string>>();

        for (var currentIndex = 0; currentIndex < mappers.Count; currentIndex++)
        {
            var current = mappers[currentIndex];

            for (var candidateIndex = currentIndex + 1; candidateIndex < mappers.Count; candidateIndex++)
            {
                var candidate = mappers[candidateIndex];
                var sharedNames = current.QueryGroupNameCandidates.Intersect(
                    candidate.QueryGroupNameCandidates,
                    StringComparer.Ordinal);

                foreach (var sharedName in sharedNames)
                {
                    AddConflict(conflicts, current, sharedName);
                    AddConflict(conflicts, candidate, sharedName);
                }
            }
        }

        foreach (var mapper in mappers)
        {
            if (!conflicts.TryGetValue(mapper, out var conflictingNames))
            {
                continue;
            }

            yield return Diagnostic.Create(
                RazQLDiagnostics.DuplicateMapperQueryGroupName,
                mapper.MapperInterface.Locations.FirstOrDefault(),
                mapper.MapperInterface.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                string.Join(", ", conflictingNames.OrderBy(static name => name, StringComparer.Ordinal)));
        }
    }

    private static void AddConflict(
        IDictionary<MapperModel, HashSet<string>> conflicts,
        MapperModel mapper,
        string queryGroupName)
    {
        if (!conflicts.TryGetValue(mapper, out var mapperConflicts))
        {
            mapperConflicts = new HashSet<string>(StringComparer.Ordinal);
            conflicts.Add(mapper, mapperConflicts);
        }

        mapperConflicts.Add(queryGroupName);
    }
}

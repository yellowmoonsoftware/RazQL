using Microsoft.CodeAnalysis;
using RazQL.Generators.Models;

namespace RazQL.Generators.Validation;

internal static class MapperQueryNameValidator
{
    public static IEnumerable<Diagnostic> Validate(
        MapperModel mapper)
    {
        var mapperName = mapper.MapperInterface.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
        var methodArray = mapper.Methods;
        var conflicts = new Dictionary<MapperMethodModel, HashSet<string>>();

        for (var currentIndex = 0; currentIndex < methodArray.Count; currentIndex++)
        {
            var current = methodArray[currentIndex];

            for (var candidateIndex = currentIndex + 1; candidateIndex < methodArray.Count; candidateIndex++)
            {
                var candidate = methodArray[candidateIndex];
                var sharedNames = current.QueryNameCandidates.Intersect(
                    candidate.QueryNameCandidates,
                    StringComparer.Ordinal);

                foreach (var sharedName in sharedNames)
                {
                    AddConflict(conflicts, current, sharedName);
                    AddConflict(conflicts, candidate, sharedName);
                }
            }
        }

        foreach (var method in methodArray)
        {
            if (!conflicts.TryGetValue(method, out var conflictingNames))
            {
                continue;
            }

            yield return Diagnostic.Create(
                RazQLDiagnostics.DuplicateMapperQueryName,
                method.Method.Locations.FirstOrDefault(),
                method.Method.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                string.Join(", ", conflictingNames.OrderBy(static name => name, StringComparer.Ordinal)),
                mapperName);
        }
    }

    private static void AddConflict(
        IDictionary<MapperMethodModel, HashSet<string>> conflicts,
        MapperMethodModel method,
        string queryName)
    {
        if (!conflicts.TryGetValue(method, out var methodConflicts))
        {
            methodConflicts = new HashSet<string>(StringComparer.Ordinal);
            conflicts.Add(method, methodConflicts);
        }

        methodConflicts.Add(queryName);
    }
}

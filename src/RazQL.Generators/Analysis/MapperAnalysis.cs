using Microsoft.CodeAnalysis;
using RazQL.Generators.Models;

namespace RazQL.Generators.Analysis;

internal sealed class MapperAnalysis
{
    internal MapperAnalysis(
        MapperModel model,
        IReadOnlyList<Diagnostic> diagnostics)
    {
        Model = model;
        Diagnostics = diagnostics;
    }

    public MapperModel Model { get; }

    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    public bool IsValid => Diagnostics.Count == 0;
}

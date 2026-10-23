using RazQL.Binding;

namespace RazQL.Dapper.Mapping;

/// <summary>Links a relationship to the child and owner positions in a Dapper multi-mapping row.</summary>
/// <param name="Relation">The property relationship to apply.</param>
/// <param name="Index">The child object's zero-based position.</param>
/// <param name="ParentIndex">The owning object's zero-based position.</param>
public record CorrelatedRelation(Relation Relation, int Index, int ParentIndex);

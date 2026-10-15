using RazQL.Binding;

namespace RazQL.Dapper.Mapping;

public record CorrelatedRelation(Relation Relation, int Index, int ParentIndex);
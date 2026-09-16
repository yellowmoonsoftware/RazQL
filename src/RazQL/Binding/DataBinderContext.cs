namespace RazQL.Binding;

/// <summary>Pairs a query's data binder with its provider-neutral parameters.</summary>
/// <typeparam name="TCriteria">The query criteria type.</typeparam>
/// <param name="DataBinder">The binder for this query.</param>
/// <param name="Parameters">The parameters populated by the binder.</param>
public sealed record DataBinderContext<TCriteria>(IDataBinder<TCriteria> DataBinder, IParameterBag Parameters);

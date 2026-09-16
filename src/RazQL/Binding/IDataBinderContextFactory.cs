namespace RazQL.Binding;

/// <summary>Creates an independent data-binding context for each query.</summary>
public interface IDataBinderContextFactory
{
    /// <summary>Creates a binder and parameter collection for the supplied criteria.</summary>
    /// <typeparam name="TCriteria">The query criteria type.</typeparam>
    /// <param name="criteria">The query criteria.</param>
    /// <returns>The query's binding context.</returns>
    DataBinderContext<TCriteria> Create<TCriteria>(TCriteria criteria);
}

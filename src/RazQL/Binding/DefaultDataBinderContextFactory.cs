using Dapper;

namespace RazQL.Binding;

/// <summary>Creates query-local default data binders and parameter collections.</summary>
/// <param name="parameterNameProviderFactory">Creates a fresh naming scope for each query.</param>
/// <param name="options">The immutable binder options shared by all queries.</param>
public sealed class DefaultDataBinderContextFactory(
    IParameterNameProviderFactory parameterNameProviderFactory,
    DataBinderOptions options) : IDataBinderContextFactory
{
    /// <inheritdoc />
    public DataBinderContext<TCriteria> Create<TCriteria>(TCriteria criteria)
    {
        var parameters = new DynamicParameters();
        var binder = new DataBinder<TCriteria>(criteria, parameters, parameterNameProviderFactory.Create(), options);
        return new DataBinderContext<TCriteria>(binder, parameters);
    }
}

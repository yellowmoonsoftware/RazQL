namespace RazQL.Binding;

/// <summary>Creates an independent parameter-name scope for each query.</summary>
public interface IParameterNameProviderFactory
{
    /// <summary>Creates a parameter-name provider with fresh naming state.</summary>
    /// <returns>The parameter-name provider for one query.</returns>
    IParameterNameProvider Create();
}

namespace RazQL.Binding;

/// <summary>Creates the default parameter-name provider for each query.</summary>
public sealed class DefaultParameterNameProviderFactory : IParameterNameProviderFactory
{
    /// <inheritdoc />
    public IParameterNameProvider Create() => new DefaultParameterNameProvider();
}

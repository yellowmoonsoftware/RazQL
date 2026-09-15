using System.Diagnostics.CodeAnalysis;

namespace RazQL;

/// <summary>Records a generated mapper implementation for runtime registration.</summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class RazQLMapperImplementationAttribute : Attribute
{
    /// <summary>Initializes a mapper-to-implementation registration.</summary>
    /// <param name="mapperType">The mapper interface type.</param>
    /// <param name="implementationType">The generated concrete implementation type.</param>
    public RazQLMapperImplementationAttribute(
        Type mapperType,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        Type implementationType)
    {
        ArgumentNullException.ThrowIfNull(mapperType);
        ArgumentNullException.ThrowIfNull(implementationType);

        MapperType = mapperType;
        ImplementationType = implementationType;
    }

    /// <summary>Gets the mapper interface type.</summary>
    public Type MapperType { get; }

    /// <summary>Gets the generated concrete implementation type.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public Type ImplementationType { get; }
}

namespace RazQL;

/// <summary>Marks an interface for RazQL validation and mapper implementation generation.</summary>
[AttributeUsage(AttributeTargets.Interface, Inherited = false)]
public sealed class RazQLMapperAttribute : Attribute
{
    /// <summary>Gets or sets an explicit template group name in place of interface-name conventions.</summary>
    public string? Name { get; set; }
}

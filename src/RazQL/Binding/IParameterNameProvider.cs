namespace RazQL.Binding;

/// <summary>Creates collision-safe names for SQL parameters within one template evaluation.</summary>
public interface IParameterNameProvider
{
    /// <summary>Identifies an item while binding an enumerated value.</summary>
    /// <param name="Index">The zero-based item index.</param>
    public record EnumeratingContext(int Index);

    /// <summary>Gets a name that is reused for repeated bindings of the same logical value.</summary>
    /// <param name="baseName">The unqualified parameter name.</param>
    /// <returns>A stable parameter name without the <c>@</c> prefix.</returns>
    string GetStableName(string baseName);

    /// <summary>Gets a new name for a transformed or otherwise distinct value.</summary>
    /// <param name="baseName">The unqualified parameter name.</param>
    /// <returns>A unique parameter name without the <c>@</c> prefix.</returns>
    string GetUniqueName(string baseName);

    /// <summary>Creates a provider that prefixes names while sharing collision state with this provider.</summary>
    /// <param name="prefix">The member-path prefix.</param>
    /// <param name="enumeratingContext">The optional collection-item context.</param>
    /// <returns>A parameter-name provider for the nested scope.</returns>
    IParameterNameProvider WithPrefix(string prefix, EnumeratingContext? enumeratingContext);
}

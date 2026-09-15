namespace RazQL.Binding;

/// <summary>Describes the position of an item selected for repeated template output.</summary>
/// <param name="Index">The zero-based item index.</param>
/// <param name="IsFirst">Whether the item is first.</param>
/// <param name="IsLast">Whether the item is last.</param>
/// <param name="Count">The total number of items.</param>
public record EnumeratingContext(int Index, bool IsFirst, bool IsLast, int Count)
{
    /// <summary>Returns a separator unless this is the last item.</summary>
    /// <param name="separator">The separator text.</param>
    /// <returns>The separator, or an empty string for the last item.</returns>
    public string Separator(string separator = ",")
    {
        return !IsLast ? separator : "";
    }
};

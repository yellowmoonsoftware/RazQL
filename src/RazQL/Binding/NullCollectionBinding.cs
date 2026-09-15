namespace RazQL.Binding;

/// <summary>Controls how a null collection is represented by <c>BindAsArray</c>.</summary>
public enum NullCollectionBinding
{
    /// <summary>Bind a database null value.</summary>
    AsNull,

    /// <summary>Bind an empty array of the collection element type.</summary>
    AsEmptyArray
}
